using MediatR;

namespace CodeSync.Application.Features.Challenges.Queries.GetChallenges;

public sealed record GetChallengesQuery(string? Difficulty = null) : IRequest<IReadOnlyList<ChallengeSummaryDto>>;

public sealed record ChallengeSummaryDto(
    string Id,
    string Title,
    string Description,
    string Difficulty,
    string Language,
    int TestCaseCount);
