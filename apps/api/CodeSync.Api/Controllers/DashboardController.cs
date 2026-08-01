using CodeSync.Api.Extensions;
using CodeSync.Application.Features.Dashboard.Queries.GetDashboard;
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
}
