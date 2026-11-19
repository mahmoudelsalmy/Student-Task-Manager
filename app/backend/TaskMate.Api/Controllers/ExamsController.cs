using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskMate.Api.Contracts;
using TaskMate.Api.Data;
using TaskMate.Api.Domain;

namespace TaskMate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/exams")]
public sealed class ExamsController(TaskMateDbContext db, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExamResponse>>> GetExams(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var exams = await db.Exams.AsNoTracking().Where(exam => exam.UserId == userId.Value)
            .OrderBy(exam => exam.ScheduledAtUtc).ToListAsync(cancellationToken);
        return Ok(exams.Select(ToResponse).ToArray());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExamResponse>> GetExam(Guid id, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var exam = await db.Exams.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value, cancellationToken);
        return exam is null ? NotFound() : Ok(ToResponse(exam));
    }

    [HttpPost]
    public async Task<ActionResult<ExamResponse>> CreateExam(ExamRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        if (request.ScheduledAtUtc == DateTimeOffset.MinValue || request.ScheduledAtUtc.Offset != TimeSpan.Zero) return Invalid("ScheduledAtUtc must be a valid UTC date and time (Z).");
        if (request.CourseId is { } courseId &&
            !await db.Courses.AnyAsync(course => course.Id == courseId && course.UserId == userId.Value, cancellationToken))
            return Invalid("The selected course does not belong to your account.");

        var exam = new Exam
        {
            UserId = userId.Value,
            CourseId = request.CourseId,
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            ScheduledAtUtc = request.ScheduledAtUtc.UtcDateTime,
            DurationMinutes = request.DurationMinutes,
            Location = Normalize(request.Location),
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetExam), new { id = exam.Id }, ToResponse(exam));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExamResponse>> UpdateExam(Guid id, ExamRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var exam = await db.Exams.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value, cancellationToken);
        if (exam is null) return NotFound();
        if (request.ScheduledAtUtc == DateTimeOffset.MinValue || request.ScheduledAtUtc.Offset != TimeSpan.Zero) return Invalid("ScheduledAtUtc must be a valid UTC date and time (Z).");
        if (request.CourseId is { } courseId &&
            !await db.Courses.AnyAsync(course => course.Id == courseId && course.UserId == userId.Value, cancellationToken))
            return Invalid("The selected course does not belong to your account.");

        exam.CourseId = request.CourseId;
        exam.Title = request.Title.Trim();
        exam.Description = Normalize(request.Description);
        exam.ScheduledAtUtc = request.ScheduledAtUtc.UtcDateTime;
        exam.DurationMinutes = request.DurationMinutes;
        exam.Location = Normalize(request.Location);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(exam));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteExam(Guid id, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var exam = await db.Exams.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value, cancellationToken);
        if (exam is null) return NotFound();
        db.Exams.Remove(exam);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid? CurrentUserId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
    private BadRequestObjectResult Invalid(string detail) => BadRequest(new ProblemDetails
    {
        Title = "Invalid exam.", Detail = detail, Status = StatusCodes.Status400BadRequest
    });
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ExamResponse ToResponse(Exam exam) => new(
        exam.Id, exam.CourseId, exam.Title, exam.Description,
        new DateTimeOffset(DateTime.SpecifyKind(exam.ScheduledAtUtc, DateTimeKind.Utc)),
        exam.DurationMinutes, exam.Location,
        new DateTimeOffset(DateTime.SpecifyKind(exam.CreatedAtUtc, DateTimeKind.Utc)));
}

