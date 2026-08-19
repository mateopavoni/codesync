using CodeSync.Api.Extensions;
using CodeSync.Application.Features.Users.Commands.UpsertUserProfile;
using CodeSync.Application.Features.Users.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSync.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UserController : ControllerBase
{
    private static readonly string[] AllowedAvatarTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;

    public UserController(IMediator mediator, IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
    }

    /// <summary>Returns the authenticated user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var result = await _mediator.Send(new GetUserProfileQuery(uid), ct);
        return Ok(result);
    }

    /// <summary>
    /// Creates or updates the authenticated user's profile.
    /// Call this on first login to initialize the Firestore document from the Firebase Auth claims.
    /// </summary>
    [HttpPost("me")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpsertProfile([FromBody] UpsertProfileRequest request, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();

        await _mediator.Send(new UpsertUserProfileCommand(
            uid,
            request.DisplayName,
            request.Email,
            request.PhotoUrl), ct);

        return NoContent();
    }

    /// <summary>
    /// Sube la foto de perfil del usuario autenticado y la sirve como archivo estático.
    /// Reemplaza cualquier foto previa (mismo nombre = mismo uid).
    /// </summary>
    [HttpPost("me/avatar")]
    [ProducesResponseType(typeof(AvatarUploadResponse), 200)]
    [ProducesResponseType(400)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0 || file.Length > 5 * 1024 * 1024 || !AllowedAvatarTypes.Contains(file.ContentType))
        {
            return BadRequest(new { error = "La imagen debe ser JPEG/PNG/WEBP/GIF de hasta 5MB." });
        }

        var uid = User.GetFirebaseUid();
        var ext = file.ContentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };

        // ponytail: archivo en disco del propio host, no bucket externo — evita depender
        // del plan Blaze de Firebase Storage. Si el deploy pasa a multi-instancia, mover
        // a un volumen compartido o storage externo.
        var avatarsDir = Path.Combine(_env.WebRootPath, "avatars");
        Directory.CreateDirectory(avatarsDir);

        foreach (var old in Directory.EnumerateFiles(avatarsDir, $"{uid}.*"))
            System.IO.File.Delete(old);

        var path = Path.Combine(avatarsDir, $"{uid}{ext}");
        await using (var stream = System.IO.File.Create(path))
            await file.CopyToAsync(stream, ct);

        return Ok(new AvatarUploadResponse($"/avatars/{uid}{ext}"));
    }
}

public sealed record AvatarUploadResponse(string PhotoUrl);

public sealed record UpsertProfileRequest(
    string DisplayName,
    string Email,
    string? PhotoUrl);
