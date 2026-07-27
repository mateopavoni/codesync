namespace CodeSync.Application.Common.Interfaces;

/// <summary>
/// Abstracts Firebase token verification so the Api project
/// does not need a direct reference to FirebaseAdmin.
/// </summary>
public interface IFirebaseTokenVerifier
{
    /// <summary>
    /// Verifies the given Firebase ID token. Returns null if the token is invalid or expired.
    /// </summary>
    Task<FirebaseTokenResult?> VerifyAsync(string idToken, CancellationToken ct = default);
}

public sealed record FirebaseTokenResult(string Uid, string? Email, string? DisplayName);
