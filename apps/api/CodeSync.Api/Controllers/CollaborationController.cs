using CodeSync.Api.Extensions;
using CodeSync.Application.Features.Rooms.Commands.CreateRoom;
using CodeSync.Application.Features.Rooms.Commands.JoinRoom;
using CodeSync.Application.Features.Rooms.Commands.SelectChallenge;
using CodeSync.Application.Features.Rooms.Queries.GetRoom;
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
    /// Creates a new collaborative room, sin desafío asignado todavía.
    /// Returns the room ID and a 6-character invite code to share with collaborators.
    /// El desafío se elige después con <see cref="SelectChallenge"/>, desde adentro de la sala.
    /// The realtime aspects (code sync, cursors, chat) are handled by Firebase Realtime DB
    /// directly from the Angular client — this endpoint only manages the room entity.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateRoomDto), 201)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> CreateRoom(CancellationToken ct)
    {
        var hostUserId = User.GetFirebaseUid();

        var result = await _mediator.Send(new CreateRoomCommand(hostUserId), ct);

        return StatusCode(201, result);
    }

    /// <summary>Estado actual de la sala (código de invitación, miembros, desafío si ya se eligió).</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RoomDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetRoom(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRoomQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Asigna el desafío elegido para la sala. Requiere ser miembro de la sala.</summary>
    [HttpPost("{id}/challenge")]
    [ProducesResponseType(typeof(SelectChallengeDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> SelectChallenge(string id, [FromBody] SelectChallengeRequest request, CancellationToken ct)
    {
        var userId = User.GetFirebaseUid();

        var result = await _mediator.Send(new SelectChallengeCommand(id, request.ChallengeId, userId), ct);
        return Ok(result);
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

public sealed record SelectChallengeRequest(string ChallengeId);
