using CodeSync.Api.Extensions;
using CodeSync.Application.Features.Rooms.Commands.CreateRoom;
using CodeSync.Application.Features.Rooms.Commands.JoinRoom;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeSync.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize]
[EnableRateLimiting("heavy")]
public sealed class CollaborationController : ControllerBase
{
    private readonly IMediator _mediator;

    public CollaborationController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Creates a new collaborative room for the given challenge.
    /// Returns the room ID and a 6-character invite code to share with collaborators.
    /// The realtime aspects (code sync, cursors, chat) are handled by Firebase Realtime DB
    /// directly from the Angular client — this endpoint only manages the room entity.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateRoomDto), 201)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request, CancellationToken ct)
    {
        var hostUserId = User.GetFirebaseUid();

        var result = await _mediator.Send(
            new CreateRoomCommand(request.ChallengeId, hostUserId), ct);

        return StatusCode(201, result);
    }

    /// <summary>
    /// Joins an existing room by invite code. Enforces the 4-user maximum server-side.
    /// Idempotent: if the user is already in the room, returns current room state.
    /// </summary>
    [HttpPost("{inviteCode}/join")]
    [ProducesResponseType(typeof(JoinRoomDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> JoinRoom(string inviteCode, CancellationToken ct)
    {
        var userId = User.GetFirebaseUid();

        var result = await _mediator.Send(new JoinRoomCommand(inviteCode, userId), ct);
        return Ok(result);
    }
}

public sealed record CreateRoomRequest(string ChallengeId);
