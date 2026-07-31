using MediatR;

namespace CodeSync.Application.Features.Rooms.Commands.JoinRoom;

public sealed record JoinRoomCommand(
    string InviteCode,
    string UserId) : IRequest<JoinRoomDto>;

public sealed record JoinRoomDto(
    string RoomId,
    string ChallengeId,
    IReadOnlyList<string> MemberIds,
    int MaxMembers);
