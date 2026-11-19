using System.ComponentModel.DataAnnotations;
namespace TaskMate.Api.Contracts;
public sealed record CreateStudySessionRequest(DateTimeOffset StartedAtUtc, DateTimeOffset EndedAtUtc, int ActualFocusSeconds, Guid? CourseId = null, Guid? TaskId = null, [param: StringLength(1000)] string? Notes = null);
public sealed record StudySessionResponse(Guid Id, Guid? CourseId, Guid? TaskId, DateTimeOffset StartedAtUtc, DateTimeOffset EndedAtUtc, int DurationMinutes, string? Notes, DateTimeOffset CreatedAtUtc);
public sealed record StudySummaryResponse(int Sessions, int TotalMinutes, int MinutesThisWeek, int MinutesThisMonth);

