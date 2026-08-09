using CodeSync.Application.Common.Extensions;
using CodeSync.Application.Common.Interfaces;
using MediatR;

namespace CodeSync.Application.Features.Challenges.Queries.GetChallenges;

internal sealed class GetChallengesHandler : IRequestHandler<GetChallengesQuery, IReadOnlyList<ChallengeSummaryDto>>
{
    private readonly IChallengeRepository _repo;

    public GetChallengesHandler(IChallengeRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ChallengeSummaryDto>> Handle(
        GetChallengesQuery request,
        CancellationToken cancellationToken)
    {
        var challenges = await _repo.GetAllActiveAsync(cancellationToken);

        return challenges
            .Where(c => string.IsNullOrEmpty(request.Difficulty) || c.Difficulty.ToApiString() == request.Difficulty)
            .OrderBy(c => c.Difficulty)
            .ThenBy(c => c.Title)
            .Select(c => new ChallengeSummaryDto(
                c.Id,
                c.Title,
                c.Description,
                c.Difficulty.ToApiString(),
                c.Language.ToApiString(),
                c.TestCases.Count(tc => tc.IsVisible)))
            .ToList();
    }
}
