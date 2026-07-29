namespace CodeSync.Infrastructure.Execution;

/// <summary>
/// Low-level contract for running a command inside an ephemeral Docker container.
/// Separated from <see cref="CodeExecutionService"/> so it can be mocked in unit tests
/// without touching the Docker daemon.
/// </summary>
public interface IDockerExecutor
{
    Task<DockerRunResult> RunAsync(
        string image,
        IReadOnlyList<string> command,
        string stdinInput,
        int timeoutSeconds,
        CancellationToken ct = default);
}

public sealed record DockerRunResult(
    string Stdout,
    string Stderr,
    bool TimedOut,
    string? Error = null);
