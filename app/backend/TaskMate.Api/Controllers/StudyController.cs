using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMate.Api.Contracts;
using TaskMate.Api.Services;

namespace TaskMate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/study")]
public sealed class StudyController(IStudySessionService studySessions) : ControllerBase
{
    [HttpGet("sessions")]
    [ProducesResponseType<IReadOnlyList<StudySessionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StudySessionResponse>>> GetSessions(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }
        if (limit is < 1 or > 100)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid session limit.",
                Detail = "Limit must be between 1 and 100.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(await studySessions.GetSessionsAsync(userId.Value, limit, cancellationToken));
    }

    [HttpGet("summary")]
    [ProducesResponseType<StudySummaryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StudySummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await studySessions.GetSummaryAsync(userId.Value, cancellationToken));
    }

    [HttpPost("sessions")]
    [ProducesResponseType<StudySessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StudySessionResponse>> CreateSession(
        CreateStudySessionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await studySessions.CreateSessionAsync(userId.Value, request, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid study session.",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return CreatedAtAction(nameof(GetSessions), new { limit = 50 }, result.Session);
    }

    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
}

