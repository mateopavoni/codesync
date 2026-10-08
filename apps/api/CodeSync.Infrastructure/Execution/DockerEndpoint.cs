namespace CodeSync.Infrastructure.Execution;

/// <summary>
/// Resolves the Docker daemon address. An explicit <c>Docker:EndpointUri</c> always
/// wins; otherwise the default is OS-specific (named pipe on Windows, unix socket
/// on Linux/macOS) so the API runs out of the box on any machine.
/// </summary>
public static class DockerEndpoint
{
    private const string WindowsPipe = "npipe://./pipe/docker_engine";
    private const string UnixSocket = "unix:///var/run/docker.sock";

    public static string Resolve(string? configured, bool? isWindows = null)
    {
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return isWindows ?? OperatingSystem.IsWindows() ? WindowsPipe : UnixSocket;
    }
}
