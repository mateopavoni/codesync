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
    private readonly IMediator _mediator;

    public UserController(IMediator mediator) => _mediator = mediator;

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
}

public sealed record UpsertProfileRequest(
    string DisplayName,
    string Email,
    string? PhotoUrl);
