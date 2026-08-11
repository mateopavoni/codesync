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

    public ChallengeController(IMediator mediator) => _mediator = mediator;

    /// <summary>Lists all active challenges (public).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ChallengeSummaryDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] string? difficulty, [FromQuery] string? language, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetChallengesQuery(difficulty, language), ct);
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
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }
}
