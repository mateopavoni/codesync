namespace CodeSync.Domain.Entities;

public sealed class Feedback
{
    public string Id { get; set; } = "";
    public string SubmissionId { get; set; } = "";
    public string ChallengeId { get; set; } = "";
    public string UserId { get; set; } = "";

    /// <summary>The AI-generated (or fallback) coaching text.</summary>
    public string CoachFeedback { get; set; } = "";

    /// <summary>True when the feedback came from the pre-generated hint bank (rate limited).</summary>
    public bool IsFallback { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
