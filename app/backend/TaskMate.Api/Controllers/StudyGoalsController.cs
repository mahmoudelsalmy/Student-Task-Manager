using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskMate.Api.Contracts;
using TaskMate.Api.Data;
using TaskMate.Api.Domain;

namespace TaskMate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/study/goals")]
public sealed class StudyGoalsController(TaskMateDbContext db, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudyGoalResponse>>> GetGoals(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var goals = await db.StudyGoals.AsNoTracking().Where(goal => goal.UserId == userId.Value)
            .OrderBy(goal => goal.CreatedAtUtc).ToListAsync(cancellationToken);
        var monday = WeekStartUtc(clock.GetUtcNow().UtcDateTime);
        var responses = new List<StudyGoalResponse>(goals.Count);
        foreach (var goal in goals)
        {
            var sessions = db.StudySessions.Where(session => session.UserId == userId.Value && session.StartedAtUtc >= monday);
            if (goal.CourseId is { } courseId) sessions = sessions.Where(session => session.CourseId == courseId);
            var minutes = await sessions.SumAsync(session => (int?)session.DurationMinutes, cancellationToken) ?? 0;
            responses.Add(ToResponse(goal, minutes));
        }
        return Ok(responses);
    }

    [HttpPost]
    public async Task<ActionResult<StudyGoalResponse>> CreateGoal(StudyGoalRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        if (request.CourseId is { } courseId &&
            !await db.Courses.AnyAsync(course => course.Id == courseId && course.UserId == userId.Value, cancellationToken))
            return BadRequest(new ProblemDetails { Title = "Invalid study goal.", Detail = "The selected course does not belong to your account.", Status = 400 });
        var goal = new StudyGoal
        {
            UserId = userId.Value, CourseId = request.CourseId, Title = request.Title.Trim(),
            TargetMinutesPerWeek = request.TargetMinutesPerWeek, CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        };
        db.StudyGoals.Add(goal);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetGoals), null, ToResponse(goal, 0));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StudyGoalResponse>> UpdateGoal(Guid id, StudyGoalRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var goal = await db.StudyGoals.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value, cancellationToken);
        if (goal is null) return NotFound();
        if (request.CourseId is { } courseId &&
            !await db.Courses.AnyAsync(course => course.Id == courseId && course.UserId == userId.Value, cancellationToken))
            return BadRequest(new ProblemDetails { Title = "Invalid study goal.", Detail = "The selected course does not belong to your account.", Status = 400 });
        goal.CourseId = request.CourseId;
        goal.Title = request.Title.Trim();
        goal.TargetMinutesPerWeek = request.TargetMinutesPerWeek;
        await db.SaveChangesAsync(cancellationToken);
        var monday = WeekStartUtc(clock.GetUtcNow().UtcDateTime);
        var sessions = db.StudySessions.Where(session => session.UserId == userId.Value && session.StartedAtUtc >= monday);
        if (goal.CourseId is { } idCourse) sessions = sessions.Where(session => session.CourseId == idCourse);
        var minutes = await sessions.SumAsync(session => (int?)session.DurationMinutes, cancellationToken) ?? 0;
        return Ok(ToResponse(goal, minutes));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteGoal(Guid id, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var goal = await db.StudyGoals.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value, cancellationToken);
        if (goal is null) return NotFound();
        db.StudyGoals.Remove(goal);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid? CurrentUserId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
    private static DateTime WeekStartUtc(DateTime now)
    {
        var monday = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
        return DateTime.SpecifyKind(monday, DateTimeKind.Utc);
    }
    private static StudyGoalResponse ToResponse(StudyGoal goal, int minutes) => new(
        goal.Id, goal.CourseId, goal.Title, goal.TargetMinutesPerWeek, minutes,
        DateTime.SpecifyKind(goal.CreatedAtUtc, DateTimeKind.Utc));
}

