using CodeSync.Application.Common.Interfaces;
using CodeSync.Application.Features.Submissions.Commands.CreateSubmission;
using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using Moq;

namespace CodeSync.Tests.Submissions;

/// <summary>
/// Unit tests for CreateSubmissionHandler focused on the Args/ExpectedOutput
/// round-trip: visible test cases must expose their input/expected values in the
/// response, hidden ones must not (that would leak a hidden test's answer).
/// </summary>
public sealed class CreateSubmissionHandlerTests
{
    private readonly Mock<IChallengeRepository> _challenges = new();
    private readonly Mock<ISubmissionRepository> _submissions = new();
    private readonly Mock<IFeedbackRepository> _feedbacks = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICodeExecutionService> _executor = new();
    private readonly Mock<IAICoachService> _coach = new();

    private CreateSubmissionHandler BuildHandler() => new(
        _challenges.Object, _submissions.Object, _feedbacks.Object,
        _users.Object, _executor.Object, _coach.Object);

    private static Challenge ChallengeWithVisibleAndHiddenTests() => new()
    {
        Id = "ch-1",
        Title = "Suma",
        Difficulty = DifficultyLevel.Easy,
        FunctionName = "solution",
        TestCases =
        [
            new TestCase { Args = "[1, 2]", ExpectedOutput = "3", IsVisible = true },
            new TestCase { Args = "[5, 5]", ExpectedOutput = "10", IsVisible = false }
        ]
    };

    [Fact]
    public async Task Handle_VisibleTestCase_IncludesArgsAndExpectedOutput()
    {
        _challenges.Setup(c => c.GetByIdAsync("ch-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChallengeWithVisibleAndHiddenTests());
        _executor.Setup(e => e.ExecuteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ProgrammingLanguage>(),
                It.IsAny<IReadOnlyList<TestCase>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodeExecutionResult(
                AllTestsPassed: true,
                TestResults:
                [
                    new TestCaseResult(0, Passed: true, ActualOutput: "3"),
                    new TestCaseResult(1, Passed: true, ActualOutput: "10")
                ],
                TimedOut: false, Error: null, ExecutionTimeMs: 12));

        var result = await BuildHandler().Handle(
            new CreateSubmissionCommand("ch-1", "u-1", "def solution(a, b): return a + b", ProgrammingLanguage.Python),
            CancellationToken.None);

        var visible = result.TestResults.Single(r => r.TestCaseIndex == 0);
        Assert.Equal("[1, 2]", visible.Args);
        Assert.Equal("3", visible.ExpectedOutput);
    }

    [Fact]
    public async Task Handle_HiddenTestCase_DoesNotLeakArgsOrExpectedOutput()
    {
        _challenges.Setup(c => c.GetByIdAsync("ch-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChallengeWithVisibleAndHiddenTests());
        _executor.Setup(e => e.ExecuteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ProgrammingLanguage>(),
                It.IsAny<IReadOnlyList<TestCase>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodeExecutionResult(
                AllTestsPassed: false,
                TestResults:
                [
                    new TestCaseResult(0, Passed: true, ActualOutput: "3"),
                    new TestCaseResult(1, Passed: false, ActualOutput: "99")
                ],
                TimedOut: false, Error: null, ExecutionTimeMs: 12));
        _coach.Setup(c => c.GetFeedbackAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DifficultyLevel>(),
                It.IsAny<ProgrammingLanguage>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<TestCaseResult>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AICoachResponse("hint", IsFallback: true, WasRateLimited: false));

        var result = await BuildHandler().Handle(
            new CreateSubmissionCommand("ch-1", "u-1", "def solution(a, b): return 99", ProgrammingLanguage.Python),
            CancellationToken.None);

        var hidden = result.TestResults.Single(r => r.TestCaseIndex == 1);
        Assert.Equal("", hidden.Args);
        Assert.Equal("", hidden.ExpectedOutput);
    }
}
