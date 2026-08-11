using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CodeSync.Infrastructure.Execution;

/// <summary>
/// Executes arbitrary code in an ephemeral, heavily-restricted Docker container.
///
/// Security controls applied to every container:
///   - NetworkMode = "none"       — no outbound or inbound network
///   - Memory = 256MB             — hard RAM cap (MemorySwap = Memory, so no swap)
///   - ReadonlyRootfs = true      — filesystem is read-only
///   - PidsLimit = 50             — prevents fork bombs
///   - Tmpfs /tmp                 — writable scratch space for interpreters, no exec bit
///   - User = "nobody"            — non-root UID inside the container
///   - Timeout = 5 s              — enforced via CancellationToken + SIGKILL
/// </summary>
public sealed class DockerExecutor : IDockerExecutor, IDisposable
{
    private readonly DockerClient _client;
    private readonly ILogger<DockerExecutor> _logger;

    public DockerExecutor(DockerClient client, ILogger<DockerExecutor> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<DockerRunResult> RunAsync(
        string image,
        IReadOnlyList<string> command,
        string stdinInput,
        int timeoutSeconds,
        CancellationToken ct = default)
    {
        string? containerId = null;

        try
        {
            // Encode program as base64 and pass via shell pipeline to avoid Windows
            // named-pipe stdin limitations (MultiplexedStream.CloseWrite() throws
            // NotSupportedException on the Docker Desktop named-pipe transport).
            var codeB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(stdinInput));
            var interpreterCmd = string.Join(" ", command); // e.g. "python3 -"
            var shellCmd = $"printf '%s' '{codeB64}' | base64 -d | {interpreterCmd}";

            // 1. Create container — no stdin needed
            CreateContainerParameters BuildCreateParams() => new()
            {
                Image = image,
                Cmd = new List<string> { "sh", "-c", shellCmd },
                AttachStdin = false,
                AttachStdout = true,
                AttachStderr = true,
                OpenStdin = false,
                StdinOnce = false,
                User = "nobody",
                HostConfig = new HostConfig
                {
                    NetworkMode = "none",
                    Memory = 256 * 1024 * 1024L,
                    MemorySwap = 256 * 1024 * 1024L,
                    ReadonlyRootfs = true,
                    PidsLimit = 50,
                    Tmpfs = new Dictionary<string, string>
                    {
                        { "/tmp", "noexec,nosuid,size=8m" }
                    }
                }
            };

            try
            {
                var createResponse = await _client.Containers.CreateContainerAsync(BuildCreateParams(), ct);
                containerId = createResponse.ID;

                // 2. Start the container
                await _client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), ct);
            }
            catch (DockerImageNotFoundException)
            {
                // Image missing on this host (fresh box, pruned cache, host migration).
                // Pull it once and retry instead of failing every submission with a
                // misleading "tests fallaron" until someone notices and pulls it by hand.
                _logger.LogWarning("Image {Image} not found locally, pulling before retrying.", image);
                await _client.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = image },
                    null,
                    new Progress<JSONMessage>(),
                    ct);

                var createResponse = await _client.Containers.CreateContainerAsync(BuildCreateParams(), ct);
                containerId = createResponse.ID;
                await _client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), ct);
            }

            // 3. Wait for the container to exit (with hard timeout)
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                await _client.Containers.WaitContainerAsync(containerId, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                _logger.LogWarning("Container {Id} timed out after {Timeout}s — sending SIGKILL.", containerId, timeoutSeconds);
                try
                {
                    await _client.Containers.KillContainerAsync(
                        containerId,
                        new ContainerKillParameters { Signal = "SIGKILL" });
                }
                catch (Exception killEx)
                {
                    _logger.LogError(killEx, "Failed to kill container {Id}.", containerId);
                }
                return new DockerRunResult("", "", TimedOut: true);
            }

            // 4. Read logs (stdout + stderr) — use tty:false overload for proper demux
            var stdoutBuf = new MemoryStream();
            var stderrBuf = new MemoryStream();

            using var logsStream = await _client.Containers.GetContainerLogsAsync(
                containerId,
                tty: false,
                new ContainerLogsParameters { ShowStdout = true, ShowStderr = true },
                ct);
            await logsStream.CopyOutputToAsync(Stream.Null, stdoutBuf, stderrBuf, ct);

            var stdout = Encoding.UTF8.GetString(stdoutBuf.ToArray());
            var stderr = Encoding.UTF8.GetString(stderrBuf.ToArray());

            return new DockerRunResult(stdout, stderr, TimedOut: false);
        }
        catch (DockerApiException ex)
        {
            _logger.LogError(ex, "Docker API error while running image {Image}.", image);
            return new DockerRunResult("", "", TimedOut: false, Error: $"Docker error: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error in DockerExecutor for image {Image}.", image);
            return new DockerRunResult("", "", TimedOut: false, Error: $"Execution error: {ex.Message}");
        }
        finally
        {
            // 5. Always remove the container — fire-and-forget to not block the response
            if (containerId != null)
            {
                _ = RemoveContainerSafeAsync(containerId);
            }
        }
    }

    private async Task RemoveContainerSafeAsync(string containerId)
    {
        try
        {
            await _client.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters { Force = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove container {Id}.", containerId);
        }
    }

    public void Dispose() => _client.Dispose();
}
