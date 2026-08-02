using CodeSync.Infrastructure.RateLimit;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeSync.Tests.RateLimit;

public sealed class InMemoryRateLimiterTests
{
    private readonly InMemoryRateLimiter _limiter = new(NullLogger<InMemoryRateLimiter>.Instance);
    private readonly TimeSpan _window = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task FirstRequest_ShouldBeAllowed()
    {
        var allowed = await _limiter.TryConsumeAsync("user:1", _window);
        Assert.True(allowed);
    }

    [Fact]
    public async Task SecondRequestWithinWindow_ShouldBeBlocked()
    {
        await _limiter.TryConsumeAsync("user:2", _window);
        var second = await _limiter.TryConsumeAsync("user:2", _window);
        Assert.False(second);
    }

    [Fact]
    public async Task DifferentKeys_AreIndependent()
    {
        var a = await _limiter.TryConsumeAsync("user:A", _window);
        var b = await _limiter.TryConsumeAsync("user:B", _window);
        Assert.True(a);
        Assert.True(b);
    }

    [Fact]
    public async Task AfterWindowExpires_RequestIsAllowed()
    {
        var tinyWindow = TimeSpan.FromMilliseconds(50);
        await _limiter.TryConsumeAsync("user:3", tinyWindow);

        await Task.Delay(100); // wait past the window

        var allowed = await _limiter.TryConsumeAsync("user:3", tinyWindow);
        Assert.True(allowed);
    }

    [Fact]
    public async Task ZeroWindow_AlwaysAllows()
    {
        var zeroWindow = TimeSpan.Zero;
        var first = await _limiter.TryConsumeAsync("user:4", zeroWindow);
        var second = await _limiter.TryConsumeAsync("user:4", zeroWindow);
        Assert.True(first);
        Assert.True(second);
    }
}
