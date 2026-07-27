using CodeSync.Application.Common.Interfaces;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;

namespace CodeSync.Infrastructure.Firebase;

internal sealed class FirebaseTokenVerifier : IFirebaseTokenVerifier
{
    private readonly ILogger<FirebaseTokenVerifier> _logger;

    public FirebaseTokenVerifier(ILogger<FirebaseTokenVerifier> logger) => _logger = logger;

    public async Task<FirebaseTokenResult?> VerifyAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var token = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, ct);

            token.Claims.TryGetValue("email", out var email);
            token.Claims.TryGetValue("name", out var name);

            return new FirebaseTokenResult(
                token.Uid,
                email?.ToString(),
                name?.ToString());
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogDebug("Firebase token verification failed: {Reason}", ex.AuthErrorCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error verifying Firebase token.");
            return null;
        }
    }
}
