using System.ComponentModel.DataAnnotations;
namespace TaskMate.Api.Contracts;
public sealed record CreateStudySessionRequest(DateTime StartedAtUtc, DateTime EndedAtUtc, Guid? CourseId = null, Guid? TaskId = null, [param: StringLength(1000)] string? Notes = null);
public sealed record StudySessionResponse(Guid Id, Guid? CourseId, Guid? TaskId, DateTime StartedAtUtc, DateTime EndedAtUtc, int DurationMinutes, string? Notes, DateTime CreatedAtUtc);
public sealed record StudySummaryResponse(int Sessions, int TotalMinutes, int MinutesThisWeek, int MinutesThisMonth);
