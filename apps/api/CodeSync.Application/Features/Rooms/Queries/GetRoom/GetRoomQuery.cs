using MediatR;

namespace CodeSync.Application.Features.Rooms.Queries.GetRoom;

public sealed record GetRoomQuery(string RoomId) : IRequest<RoomDto>;

public sealed record RoomDto(
    string Id,
    string InviteCode,
    string? ChallengeId,
    IReadOnlyList<string> MemberIds,
    int MaxMembers);
