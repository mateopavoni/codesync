using MediatR;

namespace CodeSync.Application.Features.Challenges.Queries.GetChallenges;

public sealed record GetChallengesQuery(string? Difficulty = null, string? Language = null, string? UserId = null) : IRequest<IReadOnlyList<ChallengeSummaryDto>>;

public sealed record ChallengeSummaryDto(
    string Id,
    string Title,
    string Description,
    string Difficulty,
    string Language,
    int TestCaseCount,
    // "not_started" | "in_progress" | "completed"
    string Status);
