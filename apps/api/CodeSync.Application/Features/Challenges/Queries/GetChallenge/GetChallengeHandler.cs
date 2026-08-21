using CodeSync.Application.Common.Extensions;
using CodeSync.Application.Common.Interfaces;
using MediatR;

namespace CodeSync.Application.Features.Challenges.Queries.GetChallenge;

internal sealed class GetChallengeHandler : IRequestHandler<GetChallengeQuery, ChallengeDetailDto>
{
    private readonly IChallengeRepository _repo;

    public GetChallengeHandler(IChallengeRepository repo) => _repo = repo;

    public async Task<ChallengeDetailDto> Handle(GetChallengeQuery request, CancellationToken cancellationToken)
    {
        var challenge = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("El desafío no existe.");

        var visible = challenge.TestCases
            .Where(tc => tc.IsVisible)
            .Select(tc => new VisibleTestCaseDto(tc.Args, tc.ExpectedOutput))
            .ToList();

        return new ChallengeDetailDto(
            challenge.Id,
            challenge.Title,
            challenge.Description,
            challenge.Difficulty.ToApiString(),
            challenge.Language.ToApiString(),
            challenge.FunctionName,
            challenge.SolutionTemplate,
            visible);
    }
}
