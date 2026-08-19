using CodeSync.Application.Common.Interfaces;
using MediatR;

namespace CodeSync.Application.Features.Rooms.Commands.CloseRoom;

internal sealed class CloseRoomHandler : IRequestHandler<CloseRoomCommand>
{
    private readonly IRoomRepository _rooms;

    public CloseRoomHandler(IRoomRepository rooms) => _rooms = rooms;

    public async Task Handle(CloseRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await _rooms.GetByIdAsync(request.RoomId, cancellationToken)
            ?? throw new KeyNotFoundException($"Room '{request.RoomId}' not found.");

        if (room.HostUserId != request.UserId)
            throw new InvalidOperationException("Solo el host puede cerrar la sala.");

        await _rooms.CloseAsync(room.Id, cancellationToken);
    }
}
