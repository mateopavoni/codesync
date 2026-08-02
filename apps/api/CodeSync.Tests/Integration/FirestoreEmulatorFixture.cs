using Google.Api.Gax;
using Google.Cloud.Firestore;
using System.Diagnostics;
using System.Net.Sockets;

namespace CodeSync.Tests.Integration;

/// <summary>
/// Arranges a local Firebase Firestore emulator for integration tests.
/// The emulator is started via npx firebase-tools (v15.25.1 already cached on this machine).
/// Port 8081 is used to avoid conflicts with any running dev emulator on 8080.
///
/// Lifecycle:
///   - Created once per test collection (via ICollectionFixture) — one emulator process for all tests.
///   - ClearDataAsync() is called by each test class before every test method to guarantee isolation.
///   - DisposeAsync() kills the emulator process after the entire collection finishes.
/// </summary>
public sealed class FirestoreEmulatorFixture : IAsyncLifetime
{
    private Process? _emulatorProcess;
    private string _tempDir = "";

    public FirestoreDb Db { get; private set; } = null!;

    // "demo-" prefix tells firebase-tools to run in demo mode — no Google credentials required.
    public const string ProjectId = "demo-codesync-test";

    private const int Port = 8081;
    private string EmulatorHost => $"localhost:{Port}";

    public async Task InitializeAsync()
    {
        // Create a temporary directory with the firebase.json config the emulator needs.
        _tempDir = Path.Combine(Path.GetTempPath(), $"codesync-emu-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        await File.WriteAllTextAsync(Path.Combine(_tempDir, "firebase.json"), $$"""
            {
              "emulators": {
                "firestore": {
                  "port": {{Port}},
                  "host": "localhost"
                }
              }
            }
            """);

        // On Windows, npx is a .cmd file — Process.Start with UseShellExecute=false requires
        // routing through cmd.exe so the shell can resolve the .cmd extension.
        var isWindows = OperatingSystem.IsWindows();
        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "npx",
            Arguments = isWindows
                ? $"/c npx --yes firebase-tools emulators:start --only firestore --project {ProjectId}"
                : $"--yes firebase-tools emulators:start --only firestore --project {ProjectId}",
            WorkingDirectory = _tempDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // CI=true suppresses interactive prompts.
        // Pass through JAVA_HOME so the emulator can find the JVM.
        psi.Environment["CI"] = "true";
        if (Environment.GetEnvironmentVariable("JAVA_HOME") is { } javaHome)
            psi.Environment["JAVA_HOME"] = javaHome;

        _emulatorProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start Firebase emulator process.");

        // Drain stdout/stderr in the background — not reading them causes the process to block
        // when internal buffers fill up.
        _ = _emulatorProcess.StandardOutput.ReadToEndAsync();
        _ = _emulatorProcess.StandardError.ReadToEndAsync();

        // Wait up to 120 s for the emulator port to open.
        // On first run, firebase-tools downloads the Firestore emulator JAR (~50 MB),
        // which adds 30-60 s to startup time.
        await WaitForPortAsync(timeoutSeconds: 120);

        // Tell the Google.Cloud.Firestore client to use the emulator.
        // Set the env var before building FirestoreDb so EmulatorDetection picks it up.
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", EmulatorHost);

        // EmulatorOnly bypasses OAuth — uses insecure channel credentials against the
        // local emulator host from FIRESTORE_EMULATOR_HOST.
        Db = new FirestoreDbBuilder
        {
            ProjectId = ProjectId,
            EmulatorDetection = EmulatorDetection.EmulatorOnly
        }.Build();
    }

    private async Task WaitForPortAsync(int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            if (_emulatorProcess?.HasExited == true)
                throw new InvalidOperationException(
                    "Firebase emulator process exited before the port became available. " +
                    "Check that Java 11+ is installed and firebase-tools is available via npx.");

            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync("127.0.0.1", Port).WaitAsync(TimeSpan.FromSeconds(2));

                // Give the emulator a moment to fully initialize its gRPC listener after
                // the TCP port opens — prevents "unavailable" errors on the first request.
                await Task.Delay(2000);
                return;
            }
            catch { /* port not yet open; keep polling */ }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Firestore emulator did not open port {Port} within {timeoutSeconds} s.");
    }

    /// <summary>
    /// Clears all Firestore documents via the emulator's REST flush endpoint.
    /// Called at the start of every test to ensure a clean state.
    /// </summary>
    public async Task ClearDataAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            var url = $"http://{EmulatorHost}/emulator/v1/projects/{ProjectId}/databases/(default)/documents";
            await http.DeleteAsync(url);
        }
        catch { /* best-effort — a failed clear only risks stale data, not crashes */ }
    }

    public Task DisposeAsync()
    {
        try { _emulatorProcess?.Kill(entireProcessTree: true); }
        catch { /* ignore — process may already be gone */ }

        _emulatorProcess?.Dispose();

        try { if (_tempDir != "") Directory.Delete(_tempDir, recursive: true); }
        catch { }

        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", null);
        return Task.CompletedTask;
    }
}
