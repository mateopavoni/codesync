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
    int ExecutionTimeMs);

public sealed record TestResultDto(
    int TestCaseIndex,
    bool Passed,
    string ActualOutput,
    string? Error = null);
