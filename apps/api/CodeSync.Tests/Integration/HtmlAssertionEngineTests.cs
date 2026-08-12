using CodeSync.Domain.Entities;
using CodeSync.Domain.Enums;
using CodeSync.Infrastructure.Execution;
using Docker.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSync.Tests.Integration;

/// <summary>
/// Integration test for the HTML grading path — runs the real Playwright harness
/// inside Docker (same requirement as the other Integration-tagged tests, see
/// DockerExecutorTests) and checks each supported DOM assertion type actually
/// evaluates correctly against a real Chromium DOM, not just that the C# side
/// builds a plausible-looking script.
/// </summary>
[Trait("Category", "Integration")]
public sealed class HtmlAssertionEngineTests
{
    [Fact]
    public async Task ExecuteAsync_Html_EvaluatesAllAssertionTypes()
    {
        var service = new CodeExecutionService(
            new DockerExecutor(new DockerClientConfiguration().CreateClient(), Mock.Of<ILogger<DockerExecutor>>()),
            new ConfigurationBuilder().Build(),
            Mock.Of<ILogger<CodeExecutionService>>());

        const string html = """
            <h1>Hola</h1>
            <p class="intro">Bienvenido según tu perfil</p>
            <img alt="foto" />
            <ul><li>a</li><li>b</li></ul>
            <div class="box" style="position:absolute; left:50vw; top:50vh; width:10px; height:10px; transform:translate(-50%,-50%);"></div>
            """;

        var testCases = new List<TestCase>
        {
            new() { ExpectedOutput = """{"type":"exists","selector":"h1"}""" },                                 // pasa
            new() { ExpectedOutput = """{"type":"textContains","selector":".intro","value":"según"}""" },       // pasa
            new() { ExpectedOutput = """{"type":"attribute","selector":"img","attr":"alt","value":"foto"}""" }, // pasa
            new() { ExpectedOutput = """{"type":"count","selector":"li","value":2,"comparator":"eq"}""" },      // pasa
            new() { ExpectedOutput = """{"type":"centered","selector":".box","tolerance":20}""" },              // pasa
            new() { ExpectedOutput = """{"type":"exists","selector":".no-existe"}""" },                         // falla
        };

        var result = await service.ExecuteAsync(html, "", ProgrammingLanguage.Html, testCases, CancellationToken.None);

        Assert.False(result.TimedOut);
        Assert.Null(result.Error);
        Assert.Equal(testCases.Count, result.TestResults.Count);
        for (int i = 0; i < testCases.Count - 1; i++)
        {
            Assert.True(result.TestResults[i].Passed, $"assertion {i} should have passed: {testCases[i].ExpectedOutput}");
        }
        Assert.False(result.TestResults[^1].Passed);
        Assert.False(result.AllTestsPassed);
    }
}
