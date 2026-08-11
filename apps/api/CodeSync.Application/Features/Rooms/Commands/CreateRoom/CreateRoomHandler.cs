using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using MediatR;

namespace CodeSync.Application.Features.Rooms.Commands.CreateRoom;

internal sealed class CreateRoomHandler : IRequestHandler<CreateRoomCommand, CreateRoomDto>
{
    private readonly IRoomRepository _rooms;
    private readonly IChallengeRepository _challenges;

    public CreateRoomHandler(IRoomRepository rooms, IChallengeRepository challenges)
    {
        _rooms = rooms;
        _challenges = challenges;
    }

    public async Task<CreateRoomDto> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        // Validate challenge exists
        var challenge = await _challenges.GetByIdAsync(request.ChallengeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Challenge '{request.ChallengeId}' not found.");

        var activeRoomCount = await _rooms.CountActiveByHostUserIdAsync(request.HostUserId, cancellationToken);
        if (activeRoomCount >= Room.MaxActiveRoomsPerHost)
        {
            throw new InvalidOperationException(
                $"Ya tenés {Room.MaxActiveRoomsPerHost} salas activas. Cerrá una antes de crear otra.");
        }

        var inviteCode = GenerateInviteCode();

        var room = new Room
        {
            InviteCode = inviteCode,
            ChallengeId = challenge.Id,
            HostUserId = request.HostUserId,
            MemberIds = new List<string> { request.HostUserId },
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var roomId = await _rooms.CreateAsync(room, cancellationToken);

        return new CreateRoomDto(roomId, inviteCode, challenge.Id, Room.MaxMembers);
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no ambiguous chars (0/O, 1/I)
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
