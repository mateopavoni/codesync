using CodeSync.Domain.Enums;

namespace CodeSync.Application.Common.Extensions;

/// <summary>
/// System.Text.Json serializes enums as integers by default (no JsonStringEnumConverter
/// registered), but the Angular frontend expects lowercase Spanish/English string values.
/// DTOs must expose these as strings via the methods below instead of the raw enum.
/// </summary>
public static class EnumExtensions
{
    public static string ToApiString(this DifficultyLevel difficulty) => difficulty switch
    {
        DifficultyLevel.Easy => "facil",
        DifficultyLevel.Medium => "medio",
        DifficultyLevel.Hard => "dificil",
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null),
    };

    public static string ToApiString(this ProgrammingLanguage language) => language switch
    {
        ProgrammingLanguage.Python => "python",
        ProgrammingLanguage.JavaScript => "javascript",
        ProgrammingLanguage.Html => "html",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };
}
