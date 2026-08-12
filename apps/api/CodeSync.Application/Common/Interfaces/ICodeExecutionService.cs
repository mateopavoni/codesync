using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;

namespace CodeSync.Application.Common.Interfaces;

public interface ICodeExecutionService
{
    /// <summary>
    /// Compiles and runs <paramref name="code"/> in an isolated Docker container,
    /// then validates its output against each test case.
    /// </summary>
    Task<CodeExecutionResult> ExecuteAsync(
        string code,
        string functionName,
        ProgrammingLanguage language,
        IReadOnlyList<TestCase> testCases,
        CancellationToken ct = default);
}

public sealed record CodeExecutionResult(
    bool AllTestsPassed,
    IReadOnlyList<TestCaseResult> TestResults,
    bool TimedOut,
    string? Error,
    int ExecutionTimeMs,
    string? ConsoleOutput = null);

public sealed record TestCaseResult(
    int TestCaseIndex,
    bool Passed,
    string ActualOutput,
    string? Error = null);
