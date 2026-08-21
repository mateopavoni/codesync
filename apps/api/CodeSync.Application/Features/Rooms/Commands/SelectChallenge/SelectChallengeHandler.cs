using CodeSync.Application.Common.Interfaces;
using MediatR;

namespace CodeSync.Application.Features.Rooms.Commands.SelectChallenge;

internal sealed class SelectChallengeHandler : IRequestHandler<SelectChallengeCommand, SelectChallengeDto>
{
    private readonly IRoomRepository _rooms;
    private readonly IChallengeRepository _challenges;

    public SelectChallengeHandler(IRoomRepository rooms, IChallengeRepository challenges)
    {
        _rooms = rooms;
        _challenges = challenges;
    }

    public async Task<SelectChallengeDto> Handle(SelectChallengeCommand request, CancellationToken cancellationToken)
    {
        var room = await _rooms.GetByIdAsync(request.RoomId, cancellationToken)
            ?? throw new KeyNotFoundException($"Room '{request.RoomId}' not found.");

        if (!room.IsUsable)
            throw new KeyNotFoundException("La sala no existe o ya no está disponible.");

        if (!room.MemberIds.Contains(request.UserId))
        {
            throw new InvalidOperationException("Solo los miembros de la sala pueden elegir el desafío.");
        }

        var challenge = await _challenges.GetByIdAsync(request.ChallengeId, cancellationToken)
            ?? throw new KeyNotFoundException("El desafío no existe.");

        await _rooms.UpdateChallengeAsync(room.Id, challenge.Id, cancellationToken);

        return new SelectChallengeDto(room.Id, challenge.Id);
    }
}
