using Microsoft.EntityFrameworkCore;
using TaskMate.Api.Contracts;
using TaskMate.Api.Data;
using TaskMate.Api.Domain;

namespace TaskMate.Api.Services;

public sealed class StudySessionService(TaskMateDbContext db, TimeProvider clock) : IStudySessionService
{
    public async Task<IReadOnlyList<StudySessionResponse>> GetSessionsAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken)
    {
        var sessions = await db.StudySessions.AsNoTracking()
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.StartedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return sessions.Select(Map).ToArray();
    }

    public async Task<StudySummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var utcNow = now.UtcDateTime;
        var daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
        var weekStart = DateTime.SpecifyKind(utcNow.Date.AddDays(-daysSinceMonday), DateTimeKind.Utc);
        var monthStart = DateTime.SpecifyKind(new DateTime(utcNow.Year, utcNow.Month, 1), DateTimeKind.Utc);
        var sessions = db.StudySessions.AsNoTracking().Where(session => session.UserId == userId);

        var count = await sessions.CountAsync(cancellationToken);
        var total = await sessions.SumAsync(session => (int?)session.DurationMinutes, cancellationToken) ?? 0;
        var thisWeek = await sessions.Where(session => session.StartedAtUtc >= weekStart)
            .SumAsync(session => (int?)session.DurationMinutes, cancellationToken) ?? 0;
        var thisMonth = await sessions.Where(session => session.StartedAtUtc >= monthStart)
            .SumAsync(session => (int?)session.DurationMinutes, cancellationToken) ?? 0;

        return new StudySummaryResponse(count, total, thisWeek, thisMonth);
    }

    public async Task<StudySessionCreateResult> CreateSessionAsync(
        Guid userId,
        CreateStudySessionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StartedAtUtc.Offset != TimeSpan.Zero || request.EndedAtUtc.Offset != TimeSpan.Zero)
        {
            return Invalid("StartedAtUtc and EndedAtUtc must use the UTC offset (Z).");
        }

        var startedAtUtc = request.StartedAtUtc.UtcDateTime;
        var endedAtUtc = request.EndedAtUtc.UtcDateTime;
        var elapsed = endedAtUtc - startedAtUtc;
        var now = clock.GetUtcNow();
        var actualFocus = TimeSpan.FromSeconds(request.ActualFocusSeconds);
        if (endedAtUtc > now.UtcDateTime || elapsed < TimeSpan.FromMinutes(1) || elapsed > TimeSpan.FromHours(24) ||
            actualFocus < TimeSpan.FromMinutes(1) || actualFocus > TimeSpan.FromHours(4) || actualFocus > elapsed)
        {
            return Invalid("A completed session must contain 1–240 minutes of focus, fit within its elapsed time, and cannot end in the future.");
        }

        if (request.CourseId is { } courseId &&
            !await db.Courses.AnyAsync(course => course.Id == courseId && course.UserId == userId, cancellationToken))
        {
            return Invalid("The selected course does not belong to your account.");
        }

        if (request.TaskId is { } taskId &&
            !await db.Tasks.AnyAsync(task => task.Id == taskId && task.UserId == userId, cancellationToken))
        {
            return Invalid("The selected task does not belong to your account.");
        }

        var session = new StudySession
        {
            UserId = userId,
            CourseId = request.CourseId,
            TaskId = request.TaskId,
            StartedAtUtc = startedAtUtc,
            EndedAtUtc = endedAtUtc,
            DurationMinutes = (int)Math.Round(actualFocus.TotalMinutes, MidpointRounding.AwayFromZero),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAtUtc = now.UtcDateTime
        };

        db.StudySessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return new StudySessionCreateResult(Map(session), null);
    }

    private static StudySessionCreateResult Invalid(string error) => new(null, error);

    private static StudySessionResponse Map(StudySession session) => new(
        session.Id,
        session.CourseId,
        session.TaskId,
        new DateTimeOffset(DateTime.SpecifyKind(session.StartedAtUtc, DateTimeKind.Utc)),
        new DateTimeOffset(DateTime.SpecifyKind(session.EndedAtUtc, DateTimeKind.Utc)),
        session.DurationMinutes,
        session.Notes,
        new DateTimeOffset(DateTime.SpecifyKind(session.CreatedAtUtc, DateTimeKind.Utc)));
}

