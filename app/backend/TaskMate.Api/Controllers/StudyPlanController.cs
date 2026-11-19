using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskMate.Api.Contracts;
using TaskMate.Api.Data;
using TaskMate.Api.Domain;

namespace TaskMate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/study/plan")]
public sealed class StudyPlanController(TaskMateDbContext db, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudyPlanBlockResponse>>> GetBlocks(
        [FromQuery] DateTimeOffset fromUtc,
        [FromQuery] DateTimeOffset throughUtc,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        if (fromUtc.Offset != TimeSpan.Zero || throughUtc.Offset != TimeSpan.Zero ||
            throughUtc <= fromUtc || throughUtc - fromUtc > TimeSpan.FromDays(8))
            return BadRequest(new ProblemDetails { Title = "Invalid planning range.", Detail = "Use UTC timestamps for a range up to eight days.", Status = 400 });

        var blocks = await db.StudyPlanBlocks.AsNoTracking()
            .Where(block => block.UserId == userId.Value && block.StartsAtUtc >= fromUtc.UtcDateTime && block.StartsAtUtc < throughUtc.UtcDateTime)
            .OrderBy(block => block.StartsAtUtc)
            .ToListAsync(cancellationToken);
        return Ok(blocks.Select(ToResponse).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<StudyPlanBlockResponse>> CreateBlock(StudyPlanBlockRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var error = await ValidateReferences(userId.Value, request, cancellationToken);
        if (error is not null) return BadRequest(error);

        var block = new StudyPlanBlock
        {
            UserId = userId.Value,
            CourseId = request.CourseId,
            TaskId = request.TaskId,
            Title = request.Title.Trim(),
            StartsAtUtc = request.StartsAtUtc.UtcDateTime,
            DurationMinutes = request.DurationMinutes,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        };
        db.StudyPlanBlocks.Add(block);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetBlocks), new { fromUtc = request.StartsAtUtc, throughUtc = request.StartsAtUtc.AddDays(1) }, ToResponse(block));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StudyPlanBlockResponse>> UpdateBlock(Guid id, StudyPlanBlockRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var block = await db.StudyPlanBlocks.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value, cancellationToken);
        if (block is null) return NotFound();
        var error = await ValidateReferences(userId.Value, request, cancellationToken);
        if (error is not null) return BadRequest(error);

        block.CourseId = request.CourseId;
        block.TaskId = request.TaskId;
        block.Title = request.Title.Trim();
        block.StartsAtUtc = request.StartsAtUtc.UtcDateTime;
        block.DurationMinutes = request.DurationMinutes;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(block));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBlock(Guid id, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var block = await db.StudyPlanBlocks.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value, cancellationToken);
        if (block is null) return NotFound();
        db.StudyPlanBlocks.Remove(block);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ProblemDetails?> ValidateReferences(Guid userId, StudyPlanBlockRequest request, CancellationToken cancellationToken)
    {
        if (request.StartsAtUtc == default || request.StartsAtUtc.Offset != TimeSpan.Zero)
            return new ProblemDetails { Title = "Invalid study block.", Detail = "StartsAtUtc must use the UTC offset (Z).", Status = 400 };
        if (request.CourseId is { } courseId &&
            !await db.Courses.AnyAsync(course => course.Id == courseId && course.UserId == userId, cancellationToken))
            return new ProblemDetails { Title = "Invalid study block.", Detail = "The selected course does not belong to your account.", Status = 400 };
        if (request.TaskId is { } taskId &&
            !await db.Tasks.AnyAsync(task => task.Id == taskId && task.UserId == userId, cancellationToken))
            return new ProblemDetails { Title = "Invalid study block.", Detail = "The selected task does not belong to your account.", Status = 400 };
        if (request.CourseId is not null && request.TaskId is not null &&
            !await db.Tasks.AnyAsync(task => task.Id == request.TaskId && task.UserId == userId && task.CourseId == request.CourseId, cancellationToken))
            return new ProblemDetails { Title = "Invalid study block.", Detail = "The selected task is not linked to the selected course.", Status = 400 };
        return null;
    }

    private Guid? CurrentUserId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
    private static StudyPlanBlockResponse ToResponse(StudyPlanBlock block) => new(
        block.Id, block.Title, new DateTimeOffset(DateTime.SpecifyKind(block.StartsAtUtc, DateTimeKind.Utc)),
        block.DurationMinutes, block.CourseId, block.TaskId,
        new DateTimeOffset(DateTime.SpecifyKind(block.CreatedAtUtc, DateTimeKind.Utc)));
}
