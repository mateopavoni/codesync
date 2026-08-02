using CodeSync.Application.Common.Interfaces;
using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using CodeSync.Infrastructure.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;

namespace CodeSync.Tests.Execution;

/// <summary>
/// Unit tests for CodeExecutionService. The real Docker daemon is not involved —
/// IDockerExecutor is mocked to return controlled output so we can verify
/// the test-case matching logic and error-path handling independently.
/// </summary>
public sealed class CodeExecutionServiceTests
{
    private readonly Mock<IDockerExecutor> _dockerMock = new();

    private readonly IConfiguration _config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Docker:ExecutionTimeoutSeconds", "5" }
        })
        .Build();

    private CodeExecutionService BuildService() =>
        new(_dockerMock.Object, _config, NullLogger<CodeExecutionService>.Instance);

    private static IReadOnlyList<TestCase> TwoTestCases() => new[]
    {
        new TestCase { Args = "[1, 2]", ExpectedOutput = "3", IsVisible = true },
        new TestCase { Args = "[0, 0]", ExpectedOutput = "0", IsVisible = false }
    };

    [Fact]
    public async Task Execute_AllTestsPass_ReturnsAllTestsPassed()
    {
        var runnerOutput = JsonSerializer.Serialize(new[]
        {
            new { passed = true, actualOutput = "3" },
            new { passed = true, actualOutput = "0" }
        });

        _dockerMock
            .Setup(d => d.RunAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerRunResult(runnerOutput, "", false));

        var result = await BuildService().ExecuteAsync(
            "def solution(a, b): return a + b",
            "solution", ProgrammingLanguage.Python,
            TwoTestCases());

        Assert.True(result.AllTestsPassed);
        Assert.Equal(2, result.TestResults.Count);
        Assert.All(result.TestResults, r => Assert.True(r.Passed));
    }

    [Fact]
    public async Task Execute_SomeTestsFail_ReturnsCorrectPassFailState()
    {
        var runnerOutput = JsonSerializer.Serialize(new[]
        {
            new { passed = true,  actualOutput = "3"  },
            new { passed = false, actualOutput = "99" }
        });

        _dockerMock
            .Setup(d => d.RunAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerRunResult(runnerOutput, "", false));

        var result = await BuildService().ExecuteAsync(
            "def solution(a, b): return 99",
            "solution", ProgrammingLanguage.Python,
            TwoTestCases());

        Assert.False(result.AllTestsPassed);
        Assert.True(result.TestResults[0].Passed);
        Assert.False(result.TestResults[1].Passed);
    }

    [Fact]
    public async Task Execute_DockerTimeout_ReturnsTimedOutResult()
    {
        _dockerMock
            .Setup(d => d.RunAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerRunResult("", "", TimedOut: true));

        var result = await BuildService().ExecuteAsync(
            "import time; time.sleep(999)",
            "solution", ProgrammingLanguage.Python,
            TwoTestCases());

        Assert.True(result.TimedOut);
        Assert.False(result.AllTestsPassed);
        Assert.All(result.TestResults, r => Assert.False(r.Passed));
    }

    [Fact]
    public async Task Execute_SyntaxError_ReturnsErrorResult()
    {
        // Empty stdout + stderr with traceback = interpreter error before our runner ran
        _dockerMock
            .Setup(d => d.RunAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerRunResult("", "SyntaxError: invalid syntax", false));

        var result = await BuildService().ExecuteAsync(
            "def solution(a b): return a + b",  // missing comma — syntax error
            "solution", ProgrammingLanguage.Python,
            TwoTestCases());

        Assert.False(result.AllTestsPassed);
        Assert.NotNull(result.Error);
        Assert.Contains("SyntaxError", result.Error);
    }

    [Fact]
    public async Task Execute_DockerError_ReturnsErrorResult()
    {
        _dockerMock
            .Setup(d => d.RunAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerRunResult("", "", false, Error: "Docker daemon not running."));

        var result = await BuildService().ExecuteAsync(
            "def solution(): pass",
            "solution", ProgrammingLanguage.Python,
            TwoTestCases());

        Assert.False(result.AllTestsPassed);
        Assert.Equal("Docker daemon not running.", result.Error);
    }

    [Fact]
    public async Task Execute_JavaScriptLanguage_UsesNodeImage()
    {
        string? capturedImage = null;
        _dockerMock
            .Setup(d => d.RunAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyList<string>, string, int, CancellationToken>(
                (img, _, _, _, _) => capturedImage = img)
            .ReturnsAsync(new DockerRunResult("[{\"passed\":true,\"actualOutput\":\"0\"}]", "", false));

        await BuildService().ExecuteAsync(
            "function solution(a, b) { return a + b; }",
            "solution", ProgrammingLanguage.JavaScript,
            new[] { new TestCase { Args = "[0, 0]", ExpectedOutput = "0" } });

        Assert.NotNull(capturedImage);
        Assert.Contains("node", capturedImage, StringComparison.OrdinalIgnoreCase);
    }
}
