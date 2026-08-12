using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain;
using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using MediatR;

namespace CodeSync.Application.Features.Submissions.Commands.CreateSubmission;

internal sealed class CreateSubmissionHandler : IRequestHandler<CreateSubmissionCommand, SubmissionResultDto>
{
    private readonly IChallengeRepository _challenges;
    private readonly ISubmissionRepository _submissions;
    private readonly IFeedbackRepository _feedbacks;
    private readonly IUserRepository _users;
    private readonly ICodeExecutionService _executor;
    private readonly IAICoachService _coach;

    public CreateSubmissionHandler(
        IChallengeRepository challenges,
        ISubmissionRepository submissions,
        IFeedbackRepository feedbacks,
        IUserRepository users,
        ICodeExecutionService executor,
        IAICoachService coach)
    {
        _challenges = challenges;
        _submissions = submissions;
        _feedbacks = feedbacks;
        _users = users;
        _executor = executor;
        _coach = coach;
    }

    public async Task<SubmissionResultDto> Handle(CreateSubmissionCommand request, CancellationToken cancellationToken)
    {
        // 1. Load the challenge
        var challenge = await _challenges.GetByIdAsync(request.ChallengeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Challenge '{request.ChallengeId}' not found.");

        // 2. Execute code in the sandbox
        var execution = await _executor.ExecuteAsync(
            request.Code,
            challenge.FunctionName,
            request.Language,
            challenge.TestCases,
            cancellationToken);

        // 3. Map domain status
        var status = DetermineStatus(execution);

        // 4. Build submission entity
        var submission = new Submission
        {
            ChallengeId = request.ChallengeId,
            UserId = request.UserId,
            Code = request.Code,
            Language = request.Language,
            Status = status,
            AllTestsPassed = execution.AllTestsPassed,
            ExecutionTimeMs = execution.ExecutionTimeMs,
            ErrorMessage = execution.Error,
            TestResults = execution.TestResults
                .Select(r => new SubmissionTestResult
                {
                    TestCaseIndex = r.TestCaseIndex,
                    Passed = r.Passed,
                    ActualOutput = r.ActualOutput,
                    Error = r.Error
                })
                .ToList()
        };

        var submissionId = await _submissions.CreateAsync(submission, cancellationToken);

        // 5. If all tests passed, update user progress
        if (execution.AllTestsPassed)
        {
            await UpdateUserProgressAsync(request.UserId, request.ChallengeId, cancellationToken);
        }

        // 6. If any tests failed, request AI coaching (non-blocking: don't fail the submission if coach errors)
        string? aiFeedback = null;
        bool feedbackIsFallback = false;

        if (!execution.AllTestsPassed && !execution.TimedOut && execution.Error == null)
        {
            var failedResults = execution.TestResults
                .Where(r => !r.Passed)
                .ToList();

            try
            {
                var coachResponse = await _coach.GetFeedbackAsync(
                    challenge.Id,
                    challenge.Title,
                    challenge.Difficulty,
                    request.Language,
                    request.Code,
                    failedResults,
                    request.UserId,
                    cancellationToken);

                aiFeedback = coachResponse.Feedback;
                feedbackIsFallback = coachResponse.IsFallback;

                // Persist feedback
                var feedback = new Feedback
                {
                    SubmissionId = submissionId,
                    ChallengeId = request.ChallengeId,
                    UserId = request.UserId,
                    CoachFeedback = coachResponse.Feedback,
                    IsFallback = coachResponse.IsFallback,
                    CreatedAt = DateTime.UtcNow
                };

                await _feedbacks.CreateAsync(feedback, cancellationToken);
            }
            catch (Exception ex)
            {
                // AI coaching is best-effort: log but don't fail the submission
                // In production this would go to Serilog/structured logging
                Console.Error.WriteLine($"[AICoach] Failed to get feedback: {ex.Message}");
            }
        }

        return new SubmissionResultDto(
            submissionId,
            execution.AllTestsPassed,
            execution.TestResults
                .Select(r =>
                {
                    var testCase = challenge.TestCases.ElementAtOrDefault(r.TestCaseIndex);
                    var showInputOutput = testCase is { IsVisible: true };
                    return new TestResultDto(
                        r.TestCaseIndex,
                        r.Passed,
                        r.ActualOutput,
                        r.Error,
                        Args: showInputOutput ? testCase!.Args : "",
                        ExpectedOutput: showInputOutput ? testCase!.ExpectedOutput : "",
                        IsHidden: !showInputOutput);
                })
                .ToList(),
            execution.TimedOut,
            execution.Error,
            aiFeedback,
            feedbackIsFallback,
            execution.ExecutionTimeMs,
            execution.ConsoleOutput);
    }

    private static SubmissionStatus DetermineStatus(CodeExecutionResult result)
    {
        if (result.Error != null) return SubmissionStatus.Error;
        if (result.TimedOut) return SubmissionStatus.TimedOut;
        return result.AllTestsPassed ? SubmissionStatus.Completed : SubmissionStatus.Failed;
    }

    private async Task UpdateUserProgressAsync(string userId, string challengeId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user == null) return;

        if (!user.CompletedChallengeIds.Contains(challengeId))
        {
            user.CompletedChallengeIds.Add(challengeId);
            user.Level = LevelCalculator.CalculateLevel(user.CompletedChallengeIds.Count);
            user.UpdatedAt = DateTime.UtcNow;
            await _users.UpsertAsync(user, ct);
        }
    }
}
