using CodeSync.Domain.Enums;
using MediatR;

namespace CodeSync.Application.Features.Submissions.Commands.CreateSubmission;

public sealed record CreateSubmissionCommand(
    string ChallengeId,
    string UserId,
    string Code,
    ProgrammingLanguage Language) : IRequest<SubmissionResultDto>;

public sealed record SubmissionResultDto(
    string SubmissionId,
    bool AllTestsPassed,
    IReadOnlyList<TestResultDto> TestResults,
    bool TimedOut,
    string? Error,
    string? AIFeedback,
    bool FeedbackIsFallback,
    int ExecutionTimeMs,
    string? ConsoleOutput = null);

public sealed record TestResultDto(
    int TestCaseIndex,
    bool Passed,
    string ActualOutput,
    string? Error = null,
    // Blank for hidden test cases (TestCase.IsVisible == false) — see
    // CreateSubmissionHandler, never leak a hidden test's expected answer.
    string Args = "",
    string ExpectedOutput = "",
    // True when the underlying TestCase.IsVisible == false — lets the frontend
    // group these separately instead of rendering blank Entrada/Esperado.
    bool IsHidden = false);
