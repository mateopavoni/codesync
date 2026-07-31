using CodeSync.Domain.Enums;

namespace CodeSync.Application.Common.Interfaces;

public interface IAICoachService
{
    /// <summary>
    /// Generates coaching feedback for a failed submission. Returns a fallback hint
    /// if the user has exceeded the per-minute rate limit.
    /// </summary>
    Task<AICoachResponse> GetFeedbackAsync(
        string challengeId,
        string challengeTitle,
        DifficultyLevel difficulty,
        ProgrammingLanguage language,
        string code,
        IReadOnlyList<TestCaseResult> failedTests,
        string userId,
        CancellationToken ct = default);
}

public sealed record AICoachResponse(
    string Feedback,
    bool IsFallback,
    bool WasRateLimited);
