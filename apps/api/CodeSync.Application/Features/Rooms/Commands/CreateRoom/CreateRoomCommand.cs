using MediatR;

namespace CodeSync.Application.Features.Rooms.Commands.CreateRoom;

public sealed record CreateRoomCommand(string HostUserId) : IRequest<CreateRoomDto>;

public sealed record CreateRoomDto(
    string RoomId,
    string InviteCode,
    string? ChallengeId,
    int MaxMembers);
