using System.ComponentModel.DataAnnotations;

namespace TaskMate.Api.Contracts;

public sealed record StudyPlanBlockRequest(
    [Required, StringLength(120, MinimumLength = 2), RegularExpression(@".*\S.*")] string Title,
    DateTimeOffset StartsAtUtc,
    [Range(15, 480)] int DurationMinutes,
    Guid? CourseId = null,
    Guid? TaskId = null);

public sealed record StudyPlanBlockResponse(
    Guid Id,
    string Title,
    DateTimeOffset StartsAtUtc,
    int DurationMinutes,
    Guid? CourseId,
    Guid? TaskId,
    DateTimeOffset CreatedAtUtc);
