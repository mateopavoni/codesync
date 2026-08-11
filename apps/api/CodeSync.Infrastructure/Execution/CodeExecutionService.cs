using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace CodeSync.Infrastructure.Execution;

/// <summary>
/// Builds a language-specific test runner, hands it to DockerExecutor,
/// and interprets the JSON output against the challenge's test cases.
/// </summary>
public sealed class CodeExecutionService : ICodeExecutionService
{
    private readonly IDockerExecutor _docker;
    private readonly IConfiguration _config;
    private readonly ILogger<CodeExecutionService> _logger;

    public CodeExecutionService(
        IDockerExecutor docker,
        IConfiguration config,
        ILogger<CodeExecutionService> logger)
    {
        _docker = docker;
        _config = config;
        _logger = logger;
    }

    public async Task<CodeExecutionResult> ExecuteAsync(
        string code,
        string functionName,
        ProgrammingLanguage language,
        IReadOnlyList<TestCase> testCases,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var (image, command) = GetContainerSpec(language);
        int timeout = _config.GetValue("Docker:ExecutionTimeoutSeconds", 5);

        var testCasesJson = JsonSerializer.Serialize(
            testCases.Select((tc, i) => new
            {
                index = i,
                args = tc.Args,
                expectedOutput = tc.ExpectedOutput
            }));

        var fullProgram = BuildRunner(language, functionName, code, testCasesJson);

        _logger.LogDebug("Running {Language} submission — image={Image}, testCases={Count}",
            language, image, testCases.Count);

        var result = await _docker.RunAsync(image, command, fullProgram, timeout, ct);
        sw.Stop();

        if (result.TimedOut)
        {
            return new CodeExecutionResult(
                AllTestsPassed: false,
                TestResults: BuildTimeoutResults(testCases.Count),
                TimedOut: true,
                Error: null,
                ExecutionTimeMs: (int)sw.ElapsedMilliseconds);
        }

        if (result.Error != null)
        {
            return new CodeExecutionResult(
                AllTestsPassed: false,
                TestResults: Array.Empty<TestCaseResult>(),
                TimedOut: false,
                Error: result.Error,
                ExecutionTimeMs: (int)sw.ElapsedMilliseconds);
        }

        // Stdout should be a JSON array of test results from our runner
        var stdout = result.Stdout.Trim();
        if (string.IsNullOrEmpty(stdout) && !string.IsNullOrEmpty(result.Stderr))
        {
            // Interpreter error (syntax, runtime before any output)
            var errorSnippet = TruncateError(result.Stderr);
            _logger.LogDebug("Interpreter error: {Stderr}", result.Stderr);
            return new CodeExecutionResult(
                AllTestsPassed: false,
                TestResults: BuildErrorResults(testCases.Count, errorSnippet),
                TimedOut: false,
                Error: errorSnippet,
                ExecutionTimeMs: (int)sw.ElapsedMilliseconds);
        }

        return ParseRunnerOutput(stdout, result.Stderr, sw.ElapsedMilliseconds);
    }

    private static CodeExecutionResult ParseRunnerOutput(string stdout, string stderr, long elapsedMs)
    {
        try
        {
            // ponytail: student code (console.log/print) can write to the same stdout
            // before the harness does. The harness always prints its JSON as the last
            // line, so take the last non-empty line instead of the whole stream.
            // Ceiling: breaks if student code schedules output *after* returning
            // (e.g. a stray setTimeout) — not a case the sync harness produces today.
            var jsonLine = stdout
                .Split('\n')
                .Select(line => line.Trim())
                .LastOrDefault(line => line.Length > 0) ?? stdout;

            var runnerResults = JsonSerializer.Deserialize<List<RunnerResult>>(
                jsonLine,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Empty runner output.");

            var testResults = runnerResults.Select((r, i) => new TestCaseResult(
                i,
                r.Passed,
                r.ActualOutput ?? "",
                r.Error)).ToList();

            return new CodeExecutionResult(
                AllTestsPassed: testResults.All(r => r.Passed),
                TestResults: testResults,
                TimedOut: false,
                Error: null,
                ExecutionTimeMs: (int)elapsedMs);
        }
        catch (Exception ex)
        {
            var detail = string.IsNullOrEmpty(stderr)
                ? $"Failed to parse test runner output: {ex.Message}"
                : TruncateError(stderr);

            return new CodeExecutionResult(
                AllTestsPassed: false,
                TestResults: Array.Empty<TestCaseResult>(),
                TimedOut: false,
                Error: detail,
                ExecutionTimeMs: (int)elapsedMs);
        }
    }

    private static (string image, IReadOnlyList<string> command) GetContainerSpec(ProgrammingLanguage lang) =>
        lang switch
        {
            ProgrammingLanguage.Python => (
                "python:3.12-alpine",
                new[] { "python3", "-" }),
            ProgrammingLanguage.JavaScript => (
                "node:22-alpine",
                new[] { "node", "--input-type=commonjs" }),
            _ => throw new NotSupportedException($"Language '{lang}' is not supported for execution.")
        };

    private static string BuildRunner(
        ProgrammingLanguage language,
        string functionName,
        string studentCode,
        string testCasesJson) =>
        language switch
        {
            ProgrammingLanguage.Python => BuildPythonRunner(functionName, studentCode, testCasesJson),
            ProgrammingLanguage.JavaScript => BuildJavaScriptRunner(functionName, studentCode, testCasesJson),
            _ => throw new NotSupportedException($"No runner template for language '{language}'.")
        };

    private static string BuildPythonRunner(string functionName, string code, string testCasesJson) =>
        $$"""
        import json, sys, traceback

        # --- Student code ---
        {{code}}

        # --- Test runner (injected by CodeSync) ---
        _test_cases = {{testCasesJson}}
        _results = []
        for _tc in _test_cases:
            try:
                _args = json.loads(_tc['args'])
                if not isinstance(_args, list):
                    _args = [_args]
                _actual = {{functionName}}(*_args)
                _expected = json.loads(_tc['expectedOutput'])
                _results.append({'passed': _actual == _expected, 'actualOutput': json.dumps(_actual)})
            except Exception as _e:
                _results.append({'passed': False, 'actualOutput': '', 'error': str(_e)})

        print(json.dumps(_results))
        sys.stdout.flush()
        """;

    private static string BuildJavaScriptRunner(string functionName, string code, string testCasesJson) =>
        $$"""
        // --- Student code ---
        {{code}}

        // --- Test runner (injected by CodeSync) ---
        const _testCases = {{testCasesJson}};
        const _results = _testCases.map(_tc => {
            try {
                const _args = JSON.parse(_tc.args);
                const _argList = Array.isArray(_args) ? _args : [_args];
                const _actual = {{functionName}}(..._argList);
                const _expected = JSON.parse(_tc.expectedOutput);
                return { passed: JSON.stringify(_actual) === JSON.stringify(_expected), actualOutput: JSON.stringify(_actual) };
            } catch(_e) {
                return { passed: false, actualOutput: '', error: _e.message };
            }
        });
        process.stdout.write(JSON.stringify(_results) + '\n');
        """;

    private static IReadOnlyList<TestCaseResult> BuildTimeoutResults(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new TestCaseResult(i, false, "", "Tiempo de ejecucion excedido (5s)."))
            .ToList();

    private static IReadOnlyList<TestCaseResult> BuildErrorResults(int count, string error) =>
        Enumerable.Range(0, count)
            .Select(i => new TestCaseResult(i, false, "", error))
            .ToList();

    private static string TruncateError(string stderr, int maxLength = 500) =>
        stderr.Length <= maxLength ? stderr.Trim() : stderr.Trim()[..maxLength] + "...";

    private sealed class RunnerResult
    {
        public bool Passed { get; set; }
        public string? ActualOutput { get; set; }
        public string? Error { get; set; }
    }
}
