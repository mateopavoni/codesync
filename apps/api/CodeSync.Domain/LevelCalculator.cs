namespace CodeSync.Domain;

/// <summary>
/// Single source of truth for the completed-challenges → level thresholds.
/// Used by submission handling (to set User.Level) and the dashboard (to show
/// progress toward the next level) — was previously duplicated as a private
/// method on CreateSubmissionHandler.
/// </summary>
public static class LevelCalculator
{
    public static int CalculateLevel(int completedCount) =>
        completedCount switch
        {
            < 3 => 1,
            < 7 => 2,
            < 15 => 3,
            < 25 => 4,
            _ => 5
        };

    /// <summary>Completed-challenges threshold that unlocks the *next* level, or
    /// null if already at the max level (5) — nothing left to reach.</summary>
    public static int? NextLevelThreshold(int completedCount) =>
        completedCount switch
        {
            < 3 => 3,
            < 7 => 7,
            < 15 => 15,
            < 25 => 25,
            _ => null
        };

    /// <summary>Completed-challenges threshold that unlocked the *current* level
    /// (0 for level 1). Together with <see cref="NextLevelThreshold"/>, lets the
    /// UI render a fill percentage for the current level's progress bar.</summary>
    public static int CurrentLevelFloor(int completedCount) =>
        completedCount switch
        {
            < 3 => 0,
            < 7 => 3,
            < 15 => 7,
            < 25 => 15,
            _ => 25
        };
}
