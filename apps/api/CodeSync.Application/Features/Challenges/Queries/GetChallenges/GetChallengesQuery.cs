using CodeSync.Domain.Enums;
using MediatR;

namespace CodeSync.Application.Features.Challenges.Queries.GetChallenges;

public sealed record GetChallengesQuery : IRequest<IReadOnlyList<ChallengeSummaryDto>>;

public sealed record ChallengeSummaryDto(
    string Id,
    string Title,
    string Description,
    DifficultyLevel Difficulty,
    ProgrammingLanguage Language,
    int TestCaseCount);
