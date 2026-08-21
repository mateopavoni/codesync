namespace CodeSync.Domain;

/// <summary>
/// Single source of truth for the XP → level thresholds. XP is earned per
/// completed challenge, weighted by difficulty (see DifficultyLevelExtensions.Xp).
/// Used by submission handling (to set User.Level) and the dashboard (to show
/// progress toward the next level).
/// </summary>
public static class LevelCalculator
{
    public static int CalculateLevel(int xp) =>
        xp switch
        {
            < 100 => 1,
            < 250 => 2,
            < 500 => 3,
            < 900 => 4,
            _ => 5
        };

    /// <summary>XP threshold that unlocks the *next* level, or null if already
    /// at the max level (5) — nothing left to reach.</summary>
    public static int? NextLevelThreshold(int xp) =>
        xp switch
        {
            < 100 => 100,
            < 250 => 250,
            < 500 => 500,
            < 900 => 900,
            _ => null
        };

    /// <summary>XP threshold that unlocked the *current* level (0 for level 1).
    /// Together with <see cref="NextLevelThreshold"/>, lets the UI render a fill
    /// percentage for the current level's progress bar.</summary>
    public static int CurrentLevelFloor(int xp) =>
        xp switch
        {
            < 100 => 0,
            < 250 => 100,
            < 500 => 250,
            < 900 => 500,
            _ => 900
        };
}
