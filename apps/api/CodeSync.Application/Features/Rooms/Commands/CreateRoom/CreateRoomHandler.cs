using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using MediatR;

namespace CodeSync.Application.Features.Rooms.Commands.CreateRoom;

internal sealed class CreateRoomHandler : IRequestHandler<CreateRoomCommand, CreateRoomDto>
{
    private readonly IRoomRepository _rooms;
    private readonly IRealtimeMembershipSync _rtdb;

    public CreateRoomHandler(IRoomRepository rooms, IRealtimeMembershipSync rtdb)
    {
        _rooms = rooms;
        _rtdb = rtdb;
    }

    public async Task<CreateRoomDto> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var activeRoomCount = await _rooms.CountActiveByHostUserIdAsync(request.HostUserId, cancellationToken);
        if (activeRoomCount >= Room.MaxActiveRoomsPerHost)
        {
            throw new InvalidOperationException(
                $"Ya tenés {Room.MaxActiveRoomsPerHost} salas activas. Cerrá una antes de crear otra.");
        }

        var inviteCode = GenerateInviteCode();

        // El desafío se elige después, desde adentro de la sala (ver Commands/SelectChallenge).
        var room = new Room
        {
            InviteCode = inviteCode,
            ChallengeId = null,
            HostUserId = request.HostUserId,
            MemberIds = new List<string> { request.HostUserId },
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var roomId = await _rooms.CreateAsync(room, cancellationToken);
        await _rtdb.AddMemberAsync(roomId, request.HostUserId, cancellationToken);

        return new CreateRoomDto(roomId, inviteCode, room.ChallengeId, Room.MaxMembers);
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no ambiguous chars (0/O, 1/I)
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
