using CodeSync.Domain.Enums;

namespace CodeSync.Domain.Entities;

public sealed class Challenge
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DifficultyLevel Difficulty { get; set; }
    public ProgrammingLanguage Language { get; set; }

    /// <summary>Name of the function the student must implement.</summary>
    public string FunctionName { get; set; } = "solution";

    /// <summary>Skeleton code shown in the editor as starting point.</summary>
    public string SolutionTemplate { get; set; } = "";

    public List<TestCase> TestCases { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
