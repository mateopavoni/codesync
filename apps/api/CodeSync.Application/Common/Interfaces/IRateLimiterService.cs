namespace CodeSync.Application.Common.Interfaces;

public interface IRateLimiterService
{
    /// <summary>
    /// Attempts to consume a token for the given key within the specified window.
    /// Returns true if the request is allowed, false if it is rate-limited.
    /// </summary>
    Task<bool> TryConsumeAsync(string key, TimeSpan window, CancellationToken ct = default);
}
