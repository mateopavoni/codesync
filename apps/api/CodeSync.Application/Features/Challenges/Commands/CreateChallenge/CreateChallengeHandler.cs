using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using MediatR;

namespace CodeSync.Application.Features.Challenges.Commands.CreateChallenge;

internal sealed class CreateChallengeHandler : IRequestHandler<CreateChallengeCommand, string>
{
    private readonly IChallengeRepository _repo;

    public CreateChallengeHandler(IChallengeRepository repo) => _repo = repo;

    public async Task<string> Handle(CreateChallengeCommand request, CancellationToken cancellationToken)
    {
        var challenge = new Challenge
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Difficulty = request.Difficulty,
            Language = request.Language,
            FunctionName = request.FunctionName.Trim(),
            SolutionTemplate = request.SolutionTemplate,
            TestCases = request.TestCases
                .Select(tc => new TestCase
                {
                    Args = tc.Args,
                    ExpectedOutput = tc.ExpectedOutput,
                    IsVisible = tc.IsVisible
                })
                .ToList(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        return await _repo.CreateAsync(challenge, cancellationToken);
    }
}
