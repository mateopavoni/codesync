namespace CodeSync.Domain.Entities;

public sealed class User
{
    /// <summary>Firebase Auth UID — used as document ID in Firestore.</summary>
    public string Uid { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhotoUrl { get; set; }

    /// <summary>"Student" (default) or "Admin". Admin-only actions (ej. crear challenges) lo requieren.</summary>
    public string Role { get; set; } = "Student";

    /// <summary>Total XP earned from completed challenges, weighted by difficulty.</summary>
    public int Xp { get; set; }

    /// <summary>Level derived from Xp via LevelCalculator.</summary>
    public int Level { get; set; } = 1;

    /// <summary>IDs of challenges the user has fully passed.</summary>
    public List<string> CompletedChallengeIds { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
