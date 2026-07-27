namespace CodeSync.Domain.Entities;

public sealed class User
{
    /// <summary>Firebase Auth UID — used as document ID in Firestore.</summary>
    public string Uid { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhotoUrl { get; set; }

    /// <summary>XP-based level, increments as challenges are completed.</summary>
    public int Level { get; set; } = 1;

    /// <summary>IDs of challenges the user has fully passed.</summary>
    public List<string> CompletedChallengeIds { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
