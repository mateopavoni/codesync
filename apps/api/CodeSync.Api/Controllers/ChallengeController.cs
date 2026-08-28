using CodeSync.Api.Extensions;
using CodeSync.Application.Common.Interfaces;
using CodeSync.Application.Features.Challenges.Commands.CreateChallenge;
using CodeSync.Application.Features.Challenges.Queries.GetChallenge;
using CodeSync.Application.Features.Challenges.Queries.GetChallenges;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSync.Api.Controllers;

[ApiController]
[Route("api/challenges")]
public sealed class ChallengeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _users;

    public ChallengeController(IMediator mediator, IUserRepository users)
    {
        _mediator = mediator;
        _users = users;
    }

    /// <summary>Lists all active challenges (public).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ChallengeSummaryDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] string? difficulty, [FromQuery] string? language, CancellationToken ct)
    {
        // Endpoint anónimo, pero si vino un token válido el middleware de auth ya
        // completó User igual — lo usamos para calcular el estado por usuario.
        var userId = User.Identity?.IsAuthenticated == true ? User.GetFirebaseUid() : null;
        var result = await _mediator.Send(new GetChallengesQuery(difficulty, language, userId), ct);
        return Ok(result);
    }

    /// <summary>Returns the full detail of a single challenge including visible test cases (public).</summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ChallengeDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetChallengeQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Creates a new challenge (authenticated).</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(object), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateChallengeCommand command, CancellationToken ct)
    {
        var userId = User.GetFirebaseUid();
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null || user.Role != "Admin")
            return Forbid();

        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }
}
