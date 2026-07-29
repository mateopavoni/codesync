namespace CodeSync.Domain.Entities;

/// <summary>
/// A single test case for a challenge. Args and ExpectedOutput are stored as JSON strings
/// so the execution runner can parse them dynamically regardless of the argument type.
/// </summary>
public sealed class TestCase
{
    /// <summary>JSON array of arguments to pass to the solution function. E.g. "[1, 2]" or '["hello"]'</summary>
    public string Args { get; set; } = "[]";

    /// <summary>JSON-encoded expected return value of the function. E.g. "3" or '"Fizz"' or "true"</summary>
    public string ExpectedOutput { get; set; } = "null";

    /// <summary>Whether this test case is shown to the student before submission.</summary>
    public bool IsVisible { get; set; } = true;
}
