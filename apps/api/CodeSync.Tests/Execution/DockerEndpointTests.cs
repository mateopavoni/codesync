using CodeSync.Infrastructure.Execution;

namespace CodeSync.Tests.Execution;

/// <summary>
/// The Docker daemon address default must work on any OS: a named pipe on
/// Windows, the unix socket everywhere else. An explicit setting always wins.
/// </summary>
public sealed class DockerEndpointTests
{
    [Fact]
    public void Resolve_ExplicitSetting_WinsOverOsDefault()
    {
        Assert.Equal("tcp://10.0.0.5:2375", DockerEndpoint.Resolve("tcp://10.0.0.5:2375", isWindows: false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_NotConfigured_UsesUnixSocketOnLinuxAndMac(string? configured)
    {
        Assert.Equal("unix:///var/run/docker.sock", DockerEndpoint.Resolve(configured, isWindows: false));
    }

    [Fact]
    public void Resolve_NotConfigured_UsesNamedPipeOnWindows()
    {
        Assert.Equal("npipe://./pipe/docker_engine", DockerEndpoint.Resolve(null, isWindows: true));
    }
}
