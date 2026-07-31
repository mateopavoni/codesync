using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using MediatR;

namespace CodeSync.Application.Features.Rooms.Commands.JoinRoom;

internal sealed class JoinRoomHandler : IRequestHandler<JoinRoomCommand, JoinRoomDto>
{
    private readonly IRoomRepository _rooms;

    public JoinRoomHandler(IRoomRepository rooms) => _rooms = rooms;

    public async Task<JoinRoomDto> Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await _rooms.JoinAsync(request.InviteCode, request.UserId, Room.MaxMembers, cancellationToken);
        return new JoinRoomDto(room.Id, room.ChallengeId, room.MemberIds, Room.MaxMembers);
    }
}
