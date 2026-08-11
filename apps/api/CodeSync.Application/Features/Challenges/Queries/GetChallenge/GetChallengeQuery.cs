using MediatR;

namespace CodeSync.Application.Features.Challenges.Queries.GetChallenge;

public sealed record GetChallengeQuery(string Id) : IRequest<ChallengeDetailDto>;

public sealed record ChallengeDetailDto(
    string Id,
    string Title,
    string Description,
    string Difficulty,
    string Language,
    string FunctionName,
    string SolutionTemplate,
    IReadOnlyList<VisibleTestCaseDto> VisibleTestCases);

public sealed record VisibleTestCaseDto(string Args, string ExpectedOutput);
