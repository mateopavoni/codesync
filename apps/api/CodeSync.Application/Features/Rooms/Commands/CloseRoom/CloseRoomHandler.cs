using CodeSync.Application.Common.Interfaces;
using MediatR;

namespace CodeSync.Application.Features.Rooms.Commands.CloseRoom;

internal sealed class CloseRoomHandler : IRequestHandler<CloseRoomCommand>
{
    private readonly IRoomRepository _rooms;
    private readonly IRealtimeMembershipSync _rtdb;

    public CloseRoomHandler(IRoomRepository rooms, IRealtimeMembershipSync rtdb)
    {
        _rooms = rooms;
        _rtdb = rtdb;
    }

    public async Task Handle(CloseRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await _rooms.GetByIdAsync(request.RoomId, cancellationToken)
            ?? throw new KeyNotFoundException("La sala no existe o ya no está disponible.");

        if (room.HostUserId != request.UserId)
            throw new InvalidOperationException("Solo el host puede cerrar la sala.");

        await _rooms.CloseAsync(room.Id, cancellationToken);
        await _rtdb.RemoveRoomAsync(room.Id, cancellationToken);
    }
}
