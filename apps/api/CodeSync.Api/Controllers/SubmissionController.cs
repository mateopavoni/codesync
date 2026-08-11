using CodeSync.Api.Extensions;
using CodeSync.Application.Features.Submissions.Commands.CreateSubmission;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeSync.Api.Controllers;

[ApiController]
[Route("api/submissions")]
[Authorize]
[EnableRateLimiting("heavy")]
public sealed class SubmissionController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubmissionController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Submits code for a challenge. Executes it in the Docker sandbox,
    /// runs all test cases, and triggers the AI Coach if any test fails.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubmissionResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Submit([FromBody] SubmitRequest request, CancellationToken ct)
    {
        var userId = User.GetFirebaseUid();

        var command = new CreateSubmissionCommand(
            request.ChallengeId,
            userId,
            request.Code,
            request.Language);

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}

public sealed record SubmitRequest(
    string ChallengeId,
    string Code,
    CodeSync.Domain.Enums.ProgrammingLanguage Language);
