namespace CodeSync.Domain.Enums;

public enum DifficultyLevel
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

public static class DifficultyLevelExtensions
{
    /// <summary>XP awarded for completing a challenge of this difficulty.</summary>
    public static int Xp(this DifficultyLevel difficulty) => difficulty switch
    {
        DifficultyLevel.Easy => 10,
        DifficultyLevel.Medium => 20,
        DifficultyLevel.Hard => 40,
        _ => 10
    };
}
