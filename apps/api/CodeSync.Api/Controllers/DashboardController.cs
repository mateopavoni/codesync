using CodeSync.Api.Extensions;
using CodeSync.Application.Features.Dashboard.Commands.ClearFeedbackHistory;
using CodeSync.Application.Features.Dashboard.Queries.GetDashboard;
using CodeSync.Application.Features.Leaderboard.Queries.GetLeaderboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSync.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns the authenticated user's progress dashboard:
    /// completed challenges, total available, current level,
    /// and the 5 most recent AI Coach feedback entries.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), 200)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userId = User.GetFirebaseUid();
        var result = await _mediator.Send(new GetDashboardQuery(userId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Deletes all of the authenticated user's saved AI Coach feedback history.
    /// </summary>
    [HttpDelete("feedback")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> ClearFeedbackHistory(CancellationToken ct)
    {
        var userId = User.GetFirebaseUid();
        await _mediator.Send(new ClearFeedbackHistoryCommand(userId), ct);
        return NoContent();
    }

    /// <summary>
    /// Returns the global top users ranked by XP descending. Rooms award no XP,
    /// only solo challenge completions count.
    /// </summary>
    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(LeaderboardDto), 200)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLeaderboardQuery(limit is > 0 and <= 100 ? limit : 20), ct);
        return Ok(result);
    }
}
