using CodeSync.Application.Common.Interfaces;
using CodeSync.Application.Features.Rooms.Commands.CreateRoom;
using CodeSync.Application.Features.Rooms.Commands.JoinRoom;
using CodeSync.Domain.Entities;
using Moq;

namespace CodeSync.Tests.Features;

public sealed class CreateRoomHandlerTests
{
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IChallengeRepository> _challenges = new();

    [Fact]
    public async Task Handle_ValidChallenge_ReturnsRoomWithInviteCode()
    {
        var challenge = new Challenge { Id = "ch_001", Title = "Test" };
        _challenges.Setup(r => r.GetByIdAsync("ch_001", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(challenge);

        _rooms.Setup(r => r.CountActiveByHostUserIdAsync("user_host", It.IsAny<CancellationToken>()))
              .ReturnsAsync(0);
        _rooms.Setup(r => r.CreateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync("room_001");

        var handler = new CreateRoomHandler(_rooms.Object, _challenges.Object);
        var result = await handler.Handle(new CreateRoomCommand("ch_001", "user_host"), CancellationToken.None);

        Assert.Equal("room_001", result.RoomId);
        Assert.Equal(6, result.InviteCode.Length);
        Assert.Equal(Room.MaxMembers, result.MaxMembers);

        // Verify room was created with host as the only initial member
        _rooms.Verify(r => r.CreateAsync(
            It.Is<Room>(room =>
                room.HostUserId == "user_host" &&
                room.MemberIds.Contains("user_host") &&
                room.MemberIds.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentChallenge_ThrowsKeyNotFoundException()
    {
        _challenges.Setup(r => r.GetByIdAsync("bad_id", It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Challenge?)null);

        var handler = new CreateRoomHandler(_rooms.Object, _challenges.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new CreateRoomCommand("bad_id", "user_host"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HostAtActiveRoomCap_ThrowsInvalidOperationException()
    {
        var challenge = new Challenge { Id = "ch_001", Title = "Test" };
        _challenges.Setup(r => r.GetByIdAsync("ch_001", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(challenge);
        _rooms.Setup(r => r.CountActiveByHostUserIdAsync("user_host", It.IsAny<CancellationToken>()))
              .ReturnsAsync(Room.MaxActiveRoomsPerHost);

        var handler = new CreateRoomHandler(_rooms.Object, _challenges.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new CreateRoomCommand("ch_001", "user_host"), CancellationToken.None));

        _rooms.Verify(r => r.CreateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public sealed class JoinRoomHandlerTests
{
    private readonly Mock<IRoomRepository> _rooms = new();

    [Fact]
    public async Task Handle_ValidCode_AddsUserAndReturnsDto()
    {
        var room = new Room
        {
            Id = "room_001",
            InviteCode = "ABC123",
            ChallengeId = "ch_001",
            HostUserId = "host",
            MemberIds = new List<string> { "host", "new_user" },
            IsActive = true
        };

        _rooms.Setup(r => r.JoinAsync("ABC123", "new_user", Room.MaxMembers, It.IsAny<CancellationToken>()))
              .ReturnsAsync(room);

        var handler = new JoinRoomHandler(_rooms.Object);
        var result = await handler.Handle(new JoinRoomCommand("ABC123", "new_user"), CancellationToken.None);

        Assert.Equal("room_001", result.RoomId);
        Assert.Contains("new_user", result.MemberIds);
    }

    [Fact]
    public async Task Handle_AlreadyMember_IsIdempotent()
    {
        var room = new Room
        {
            Id = "room_001",
            InviteCode = "ABC123",
            ChallengeId = "ch_001",
            HostUserId = "host",
            MemberIds = new List<string> { "host", "existing_user" },
            IsActive = true
        };

        _rooms.Setup(r => r.JoinAsync("ABC123", "existing_user", Room.MaxMembers, It.IsAny<CancellationToken>()))
              .ReturnsAsync(room);

        var handler = new JoinRoomHandler(_rooms.Object);
        var result = await handler.Handle(new JoinRoomCommand("ABC123", "existing_user"), CancellationToken.None);

        Assert.Contains("existing_user", result.MemberIds);
    }

    [Fact]
    public async Task Handle_FullRoom_ThrowsInvalidOperationException()
    {
        _rooms.Setup(r => r.JoinAsync("ABC123", "new_user", Room.MaxMembers, It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException($"Room is full. Maximum {Room.MaxMembers} users allowed."));

        var handler = new JoinRoomHandler(_rooms.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new JoinRoomCommand("ABC123", "new_user"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidCode_ThrowsKeyNotFoundException()
    {
        _rooms.Setup(r => r.JoinAsync("BADCOD", "user", Room.MaxMembers, It.IsAny<CancellationToken>()))
              .ThrowsAsync(new KeyNotFoundException("Room with invite code 'BADCOD' not found."));

        var handler = new JoinRoomHandler(_rooms.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new JoinRoomCommand("BADCOD", "user"), CancellationToken.None));
    }
}
