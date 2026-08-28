using MediatR;

namespace CodeSync.Application.Features.Rooms.Queries.GetRoom;

public sealed record GetRoomQuery(string RoomId, string UserId) : IRequest<RoomDto>;

public sealed record RoomDto(
    string Id,
    string InviteCode,
    string? ChallengeId,
    IReadOnlyList<string> MemberIds,
    int MaxMembers,
    string HostUserId);
