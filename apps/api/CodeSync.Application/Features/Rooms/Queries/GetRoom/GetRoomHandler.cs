using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using MediatR;

namespace CodeSync.Application.Features.Rooms.Queries.GetRoom;

internal sealed class GetRoomHandler : IRequestHandler<GetRoomQuery, RoomDto>
{
    private readonly IRoomRepository _rooms;

    public GetRoomHandler(IRoomRepository rooms) => _rooms = rooms;

    public async Task<RoomDto> Handle(GetRoomQuery request, CancellationToken cancellationToken)
    {
        var room = await _rooms.GetByIdAsync(request.RoomId, cancellationToken)
            ?? throw new KeyNotFoundException($"Room '{request.RoomId}' not found.");

        if (!room.IsUsable)
            throw new KeyNotFoundException("La sala no existe o ya no está disponible.");

        return new RoomDto(room.Id, room.InviteCode, room.ChallengeId, room.MemberIds, Room.MaxMembers, room.HostUserId);
    }
}
