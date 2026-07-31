using CodeSync.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CodeSync.Infrastructure.RateLimit;

/// <summary>
/// Simple in-process rate limiter backed by a dictionary.
/// One token per key per sliding window.
///
/// Trade-off: not horizontally scalable (state lives in-process).
/// For a portfolio MVP running on a single instance, this is sufficient.
/// In production, replace with a Redis-backed implementation (INCR + EXPIRE).
/// </summary>
public sealed class InMemoryRateLimiter : IRateLimiterService
{
    private readonly Dictionary<string, DateTime> _lastAllowed = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<InMemoryRateLimiter> _logger;

    public InMemoryRateLimiter(ILogger<InMemoryRateLimiter> logger) => _logger = logger;

    public async Task<bool> TryConsumeAsync(string key, TimeSpan window, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;

            if (_lastAllowed.TryGetValue(key, out var lastTime) && now - lastTime < window)
            {
                var remaining = window - (now - lastTime);
                _logger.LogDebug("Rate limit: key={Key} blocked, retry in {Seconds:F1}s.", key, remaining.TotalSeconds);
                return false;
            }

            _lastAllowed[key] = now;

            // Prune stale entries periodically (every 1000 writes) to avoid unbounded growth
            if (_lastAllowed.Count % 1000 == 0)
                PruneExpired(now, window);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private void PruneExpired(DateTime now, TimeSpan window)
    {
        var expired = _lastAllowed
            .Where(kv => now - kv.Value > window * 2)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expired)
            _lastAllowed.Remove(key);

        if (expired.Count > 0)
            _logger.LogDebug("RateLimiter pruned {Count} expired entries.", expired.Count);
    }
}
