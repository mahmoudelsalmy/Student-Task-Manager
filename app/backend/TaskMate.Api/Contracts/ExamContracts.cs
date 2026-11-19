using System.ComponentModel.DataAnnotations;

namespace TaskMate.Api.Contracts;

public sealed record ExamRequest(
    [Required, StringLength(120, MinimumLength = 2), RegularExpression(@".*\S.*")] string Title,
    [StringLength(2000)] string? Description,
    DateTimeOffset ScheduledAtUtc,
    [Range(15, 600)] int DurationMinutes = 120,
    Guid? CourseId = null,
    [StringLength(120)] string? Location = null);

public sealed record ExamResponse(
    Guid Id,
    Guid? CourseId,
    string Title,
    string? Description,
    DateTimeOffset ScheduledAtUtc,
    int DurationMinutes,
    string? Location,
    DateTimeOffset CreatedAtUtc);

