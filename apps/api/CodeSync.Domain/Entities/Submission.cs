using CodeSync.Domain.Enums;

namespace CodeSync.Domain.Entities;

public sealed class Submission
{
    public string Id { get; set; } = "";
    public string ChallengeId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Code { get; set; } = "";
    public ProgrammingLanguage Language { get; set; }
    public SubmissionStatus Status { get; set; }
    public bool AllTestsPassed { get; set; }
    public List<SubmissionTestResult> TestResults { get; set; } = new();
    public int ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public sealed class SubmissionTestResult
{
    public int TestCaseIndex { get; set; }
    public bool Passed { get; set; }
    public string ActualOutput { get; set; } = "";
    public string? Error { get; set; }
}
