using System.Security.Claims;

namespace CodeSync.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the Firebase UID from the authenticated user's claims.
    /// Throws <see cref="UnauthorizedAccessException"/> if the claim is missing
    /// (should never happen if [Authorize] is applied and the auth handler ran successfully).
    /// </summary>
    public static string GetFirebaseUid(this ClaimsPrincipal user)
    {
        return user.FindFirstValue("firebase_uid")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("firebase_uid claim not found in principal.");
    }
}
