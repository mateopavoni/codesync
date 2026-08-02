using CodeSync.Application.Common.Interfaces;
using CodeSync.Application.Features.Challenges.Queries.GetChallenge;
using CodeSync.Application.Features.Challenges.Queries.GetChallenges;
using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using Moq;

namespace CodeSync.Tests.Features;

public sealed class GetChallengeHandlerTests
{
    private readonly Mock<IChallengeRepository> _repo = new();

    [Fact]
    public async Task Handle_ExistingId_ReturnsDetail()
    {
        var challenge = new Challenge
        {
            Id = "ch_001",
            Title = "Test Challenge",
            Description = "Desc",
            Difficulty = DifficultyLevel.Easy,
            Language = ProgrammingLanguage.Python,
            FunctionName = "solution",
            SolutionTemplate = "def solution(): pass",
            TestCases = new List<TestCase>
            {
                new() { Args = "[]", ExpectedOutput = "0", IsVisible = true },
                new() { Args = "[]", ExpectedOutput = "0", IsVisible = false }
            }
        };

        _repo.Setup(r => r.GetByIdAsync("ch_001", It.IsAny<CancellationToken>()))
             .ReturnsAsync(challenge);

        var handler = new GetChallengeHandler(_repo.Object);
        var result = await handler.Handle(new GetChallengeQuery("ch_001"), CancellationToken.None);

        Assert.Equal("ch_001", result.Id);
        Assert.Equal("Test Challenge", result.Title);
        // Only visible test cases are returned to the client
        Assert.Single(result.VisibleTestCases);
    }

    [Fact]
    public async Task Handle_NonExistentId_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
             .ReturnsAsync((Challenge?)null);

        var handler = new GetChallengeHandler(_repo.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new GetChallengeQuery("missing"), CancellationToken.None));
    }
}

public sealed class GetChallengesHandlerTests
{
    private readonly Mock<IChallengeRepository> _repo = new();

    [Fact]
    public async Task Handle_ReturnsChallengesOrderedByDifficultyThenTitle()
    {
        var challenges = new List<Challenge>
        {
            new() { Id = "b", Title = "Banana", Difficulty = DifficultyLevel.Medium, Language = ProgrammingLanguage.Python, TestCases = new() },
            new() { Id = "a", Title = "Apple", Difficulty = DifficultyLevel.Easy, Language = ProgrammingLanguage.Python, TestCases = new() },
            new() { Id = "c", Title = "Cherry", Difficulty = DifficultyLevel.Easy, Language = ProgrammingLanguage.Python, TestCases = new() }
        };

        _repo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(challenges);

        var handler = new GetChallengesHandler(_repo.Object);
        var result = await handler.Handle(new GetChallengesQuery(), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].Title);   // Easy A
        Assert.Equal("Cherry", result[1].Title);  // Easy C
        Assert.Equal("Banana", result[2].Title);  // Medium
    }
}
