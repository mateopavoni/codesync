using CodeSync.Infrastructure.Execution;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSync.Tests.Integration;

/// <summary>
/// Integration tests for <see cref="DockerExecutor"/> against a real Docker daemon
/// (same requirement as running the app locally — see RUN.md).
///
/// Regression test for the bug where a missing sandbox image (fresh host, pruned
/// cache, host migration) made every submission fail with a misleading "algunos
/// tests fallaron" instead of actually running the code.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DockerExecutorTests
{
    [Fact]
    public async Task RunAsync_PullsMissingImage_AndStillExecutes()
    {
        using var setupClient = new DockerClientConfiguration().CreateClient();
        try
        {
            await setupClient.Images.DeleteImageAsync("node:22-alpine", new ImageDeleteParameters { Force = true });
        }
        catch (DockerImageNotFoundException)
        {
            // Already gone — that's the scenario we want.
        }

        using var client = new DockerClientConfiguration().CreateClient();
        var executor = new DockerExecutor(client, Mock.Of<ILogger<DockerExecutor>>());

        var result = await executor.RunAsync(
            "node:22-alpine",
            new[] { "node", "--input-type=commonjs" },
            "console.log(1 + 2)",
            timeoutSeconds: 30);

        Assert.Null(result.Error);
        Assert.False(result.TimedOut);
        Assert.Equal("3", result.Stdout.Trim());
    }
}
