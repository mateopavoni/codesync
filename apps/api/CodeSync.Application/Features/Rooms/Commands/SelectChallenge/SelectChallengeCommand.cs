using MediatR;

namespace CodeSync.Application.Features.Rooms.Commands.SelectChallenge;

public sealed record SelectChallengeCommand(
    string RoomId,
    string ChallengeId,
    string UserId) : IRequest<SelectChallengeDto>;

public sealed record SelectChallengeDto(string RoomId, string ChallengeId);
