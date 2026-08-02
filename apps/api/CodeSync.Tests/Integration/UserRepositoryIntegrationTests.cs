using CodeSync.Domain.Entities;
using CodeSync.Infrastructure.Firestore.Repositories;

namespace CodeSync.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="UserFirestoreRepository"/> against the Firestore emulator.
///
/// Key behavior exercised:
///   - UpsertAsync is idempotent: calling it multiple times with the same data never errors and
///     results in a single correct document.
///   - UpdateAsync: upserting an existing user overwrites the document with the new values.
/// </summary>
[Collection("Firestore Integration")]
[Trait("Category", "Integration")]
public sealed class UserRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly FirestoreEmulatorFixture _fixture;
    private readonly UserFirestoreRepository _repo;

    public UserRepositoryIntegrationTests(FirestoreEmulatorFixture fixture)
    {
        _fixture = fixture;
        _repo = new UserFirestoreRepository(fixture.Db);
    }

    public Task InitializeAsync() => _fixture.ClearDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Create (first upsert) ─────────────────────────────────────────────────

    [Fact]
    public async Task Upsert_NewUser_CreatesDocumentWithCorrectFields()
    {
        // Arrange
        var uid = $"uid_{Guid.NewGuid():N}";
        var user = new User
        {
            Uid = uid,
            DisplayName = "Mateo Test",
            Email = "mateo.test@example.com",
            PhotoUrl = "https://example.com/photo.jpg",
            Level = 1,
            CompletedChallengeIds = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _repo.UpsertAsync(user);
        var fetched = await _repo.GetByIdAsync(uid);

        // Assert
        Assert.NotNull(fetched);
        Assert.Equal(uid, fetched!.Uid);
        Assert.Equal("Mateo Test", fetched.DisplayName);
        Assert.Equal("mateo.test@example.com", fetched.Email);
        Assert.Equal("https://example.com/photo.jpg", fetched.PhotoUrl);
        Assert.Equal(1, fetched.Level);
        Assert.Empty(fetched.CompletedChallengeIds);
    }

    // ── Update (second upsert with new data) ──────────────────────────────────

    [Fact]
    public async Task Upsert_ExistingUser_OverwritesWithNewValues()
    {
        var uid = $"uid_{Guid.NewGuid():N}";

        // First upsert: level 1, no completed challenges.
        await _repo.UpsertAsync(new User
        {
            Uid = uid,
            DisplayName = "Before",
            Email = "before@example.com",
            Level = 1,
            CompletedChallengeIds = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Second upsert: level 3, two completed challenges.
        var updated = new User
        {
            Uid = uid,
            DisplayName = "After",
            Email = "after@example.com",
            Level = 3,
            CompletedChallengeIds = new List<string> { "ch_001", "ch_002" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repo.UpsertAsync(updated);

        var fetched = await _repo.GetByIdAsync(uid);

        Assert.NotNull(fetched);
        Assert.Equal("After", fetched!.DisplayName);
        Assert.Equal("after@example.com", fetched.Email);
        Assert.Equal(3, fetched.Level);
        Assert.Equal(2, fetched.CompletedChallengeIds.Count);
        Assert.Contains("ch_001", fetched.CompletedChallengeIds);
        Assert.Contains("ch_002", fetched.CompletedChallengeIds);
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Upsert_SameUserCalledTwice_IsIdempotent()
    {
        var uid = $"uid_{Guid.NewGuid():N}";
        var user = new User
        {
            Uid = uid,
            DisplayName = "Idempotent User",
            Email = "idem@example.com",
            Level = 2,
            CompletedChallengeIds = new List<string> { "ch_001" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Calling UpsertAsync twice with the same data must not throw
        // and must result in the same document state.
        await _repo.UpsertAsync(user);
        await _repo.UpsertAsync(user); // second call — should be a no-op from a data perspective

        var fetched = await _repo.GetByIdAsync(uid);

        Assert.NotNull(fetched);
        Assert.Equal("Idempotent User", fetched!.DisplayName);
        Assert.Equal(2, fetched.Level);
        Assert.Single(fetched.CompletedChallengeIds, "ch_001");
    }

    // ── Get non-existent ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_NonExistentUser_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync("uid-that-does-not-exist");
        Assert.Null(result);
    }
}
