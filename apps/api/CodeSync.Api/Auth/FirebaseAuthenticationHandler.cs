using CodeSync.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CodeSync.Api.Auth;

/// <summary>
/// ASP.NET Core authentication handler that validates Firebase ID Tokens sent as
/// Bearer tokens in the Authorization header.
///
/// Flow:
///   - No Authorization header → AuthenticateResult.NoResult (anonymous access allowed)
///   - Invalid/malformed header → AuthenticateResult.Fail (401)
///   - Valid token → sets ClaimsPrincipal with firebase_uid, email, name claims
/// </summary>
public sealed class FirebaseAuthenticationHandler : AuthenticationHandler<FirebaseAuthenticationOptions>
{
    private readonly IFirebaseTokenVerifier _verifier;

    public FirebaseAuthenticationHandler(
        IOptionsMonitor<FirebaseAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IFirebaseTokenVerifier verifier)
        : base(options, logger, encoder)
    {
        _verifier = verifier;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult(); // public endpoints remain accessible

        var raw = authHeader.ToString();
        if (!raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Authorization header must use the Bearer scheme.");

        var idToken = raw["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(idToken))
            return AuthenticateResult.Fail("Bearer token is empty.");

        var tokenResult = await _verifier.VerifyAsync(idToken, Context.RequestAborted);
        if (tokenResult == null)
            return AuthenticateResult.Fail("Invalid or expired Firebase token.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, tokenResult.Uid),
            new Claim("firebase_uid", tokenResult.Uid),
            new Claim(ClaimTypes.Email, tokenResult.Email ?? ""),
            new Claim(ClaimTypes.Name, tokenResult.DisplayName ?? "")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}

public sealed class FirebaseAuthenticationOptions : AuthenticationSchemeOptions { }
