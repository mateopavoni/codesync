using CodeSync.Application.Features.Rooms.Commands.JoinRoom;
using CodeSync.Domain.Entities;
using CodeSync.Infrastructure.Firestore.Repositories;

namespace CodeSync.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="RoomFirestoreRepository"/> against the Firestore emulator.
///
/// Key business rule exercised: max 4 members per room.
///
/// Concurrency note:
///   <see cref="RoomFirestoreRepository.JoinAsync"/> wraps the read-check-write in a Firestore
///   transaction, so concurrent joins racing for the same slot are serialized by Firestore — no
///   last-write-wins loss. The concurrent test below still asserts the count-cap safety property;
///   with the transaction in place it now also holds for liveness (a rejected joiner gets a real
///   InvalidOperationException instead of a silently dropped write).
/// </summary>
[Collection("Firestore Integration")]
[Trait("Category", "Integration")]
public sealed class RoomRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly FirestoreEmulatorFixture _fixture;
    private readonly RoomFirestoreRepository _repo;

    public RoomRepositoryIntegrationTests(FirestoreEmulatorFixture fixture)
    {
        _fixture = fixture;
        _repo = new RoomFirestoreRepository(fixture.Db);
    }

    public Task InitializeAsync() => _fixture.ClearDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Create + Get ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAndGetById_RoundTrip_AllFieldsPreserved()
    {
        var room = MakeRoom("ABCD12");

        var id = await _repo.CreateAsync(room);
        var fetched = await _repo.GetByIdAsync(id);

        Assert.NotNull(fetched);
        Assert.Equal(id, fetched!.Id);
        Assert.Equal("ABCD12", fetched.InviteCode);
        Assert.Equal("ch_001", fetched.ChallengeId);
        Assert.Equal("host_uid", fetched.HostUserId);
        Assert.Single(fetched.MemberIds, "host_uid");
        Assert.True(fetched.IsActive);
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync("room-does-not-exist");
        Assert.Null(result);
    }

    // ── Get by Invite Code ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByInviteCode_ReturnsCorrectRoom()
    {
        await _repo.CreateAsync(MakeRoom("OTHER1"));
        var targetRoom = MakeRoom("TARGET");
        var targetId = await _repo.CreateAsync(targetRoom);

        var found = await _repo.GetByInviteCodeAsync("TARGET");

        Assert.NotNull(found);
        Assert.Equal(targetId, found!.Id);
        Assert.Equal("TARGET", found.InviteCode);
    }

    [Fact]
    public async Task GetByInviteCode_InvalidCode_ReturnsNull()
    {
        var result = await _repo.GetByInviteCodeAsync("ZZZZZZ");
        Assert.Null(result);
    }

    // ── Sequential join — business rule enforcement ──────────────────────────

    /// <summary>
    /// Uses <see cref="JoinRoomHandler"/> (the real application handler) backed by the real
    /// Firestore emulator to verify the max-4-members rule end-to-end without mocks.
    ///
    /// Happy path: users 2, 3, 4 join sequentially — all succeed.
    /// 5th user: handler throws InvalidOperationException because the room is full.
    /// </summary>
    [Fact]
    public async Task JoinRoom_SequentialJoins_FourSucceed_FifthThrows()
    {
        // Arrange: room with host (1 member, 3 slots open).
        var inviteCode = UniqueCode("SEQ");
        var room = MakeRoom(inviteCode);
        await _repo.CreateAsync(room);

        var handler = new JoinRoomHandler(_repo);
        var ct = CancellationToken.None;

        // Act: 3 more users join (total 4).
        var r2 = await handler.Handle(new JoinRoomCommand(inviteCode, "user_2"), ct);
        var r3 = await handler.Handle(new JoinRoomCommand(inviteCode, "user_3"), ct);
        var r4 = await handler.Handle(new JoinRoomCommand(inviteCode, "user_4"), ct);

        // Assert: all three succeeded and returned valid DTOs.
        Assert.Equal(2, r2.MemberIds.Count);
        Assert.Equal(3, r3.MemberIds.Count);
        Assert.Equal(4, r4.MemberIds.Count);

        // Verify persisted room has exactly MaxMembers members.
        var fullRoom = await _repo.GetByInviteCodeAsync(inviteCode);
        Assert.NotNull(fullRoom);
        Assert.Equal(Room.MaxMembers, fullRoom!.MemberIds.Count);
        Assert.Contains("host_uid", fullRoom.MemberIds);
        Assert.Contains("user_4", fullRoom.MemberIds);

        // 5th join must be rejected.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new JoinRoomCommand(inviteCode, "user_5"), ct));
        Assert.Contains("full", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Idempotency: joining a room you're already in returns current state without modifying the room.
    /// </summary>
    [Fact]
    public async Task JoinRoom_AlreadyMember_IsIdempotent()
    {
        var inviteCode = UniqueCode("IDP");
        await _repo.CreateAsync(MakeRoom(inviteCode));

        var handler = new JoinRoomHandler(_repo);

        // First join (adds user_2).
        var r1 = await handler.Handle(new JoinRoomCommand(inviteCode, "user_2"), CancellationToken.None);
        Assert.Equal(2, r1.MemberIds.Count);

        // Second join with the same user — idempotent.
        var r2 = await handler.Handle(new JoinRoomCommand(inviteCode, "user_2"), CancellationToken.None);
        Assert.Equal(2, r2.MemberIds.Count); // count unchanged

        // Verify Firestore doc was NOT written a second time (count still 2).
        var room = await _repo.GetByInviteCodeAsync(inviteCode);
        Assert.Equal(2, room!.MemberIds.Count);
    }

    // ── Concurrent join — safety property ────────────────────────────────────

    /// <summary>
    /// Safety property: MaxMembers is NEVER exceeded, even when concurrent JoinRoom requests
    /// race against a room with one open slot.
    ///
    /// Implementation note: without Firestore transactions, last-write-wins means one joiner
    /// is silently lost (liveness issue), but the count cap is preserved because each write
    /// carries exactly MaxMembers members in its snapshot.
    /// </summary>
    [Fact]
    public async Task JoinRoom_Concurrent_TwoJoinersOneSlot_MaxMembersNeverExceeded()
    {
        // Arrange: 3 members, 1 slot open.
        var inviteCode = UniqueCode("CON");
        var room = new Room
        {
            InviteCode = inviteCode,
            ChallengeId = "ch_001",
            HostUserId = "host_uid",
            MemberIds = new List<string> { "host_uid", "user_2", "user_3" },
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.CreateAsync(room);

        var handler = new JoinRoomHandler(_repo);

        // Act: two concurrent joins competing for the single remaining slot.
        // One or both may succeed depending on read-write interleaving (timing-dependent).
        // We capture InvalidOperationException in case one sees the full room sequentially.
        var t1 = TryJoin(handler, inviteCode, "concurrent_A");
        var t2 = TryJoin(handler, inviteCode, "concurrent_B");
        await Task.WhenAll(t1, t2);

        // Assert: the room count must never exceed MaxMembers regardless of interleaving.
        var finalRoom = await _repo.GetByInviteCodeAsync(inviteCode);
        Assert.NotNull(finalRoom);
        Assert.True(
            finalRoom!.MemberIds.Count <= Room.MaxMembers,
            $"Room violated MaxMembers: found {finalRoom.MemberIds.Count} members, max is {Room.MaxMembers}.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Room MakeRoom(string inviteCode) => new()
    {
        InviteCode = inviteCode,
        ChallengeId = "ch_001",
        HostUserId = "host_uid",
        MemberIds = new List<string> { "host_uid" },
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Generates a unique 6-char invite code using a prefix + Guid suffix.
    /// Ensures no collision across tests in the same emulator run.
    /// </summary>
    private static string UniqueCode(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant();
        return (prefix + suffix)[..6];
    }

    /// <summary>
    /// Runs a single JoinRoom and swallows InvalidOperationException (room full / sequential case).
    /// Returns true if the join succeeded, false if rejected as full.
    /// </summary>
    private static async Task<bool> TryJoin(JoinRoomHandler handler, string inviteCode, string userId)
    {
        try
        {
            await handler.Handle(new JoinRoomCommand(inviteCode, userId), CancellationToken.None);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false; // Sequential case: one handler already completed and filled the slot
        }
    }
}
