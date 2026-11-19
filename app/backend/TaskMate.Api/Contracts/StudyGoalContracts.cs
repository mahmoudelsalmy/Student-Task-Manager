using System.ComponentModel.DataAnnotations;

namespace TaskMate.Api.Contracts;

public sealed record StudyGoalRequest(
    [Required, StringLength(100, MinimumLength = 2), RegularExpression(@".*\S.*")] string Title,
    [Range(30, 3000)] int TargetMinutesPerWeek,
    Guid? CourseId = null);

public sealed record StudyGoalResponse(
    Guid Id,
    Guid? CourseId,
    string Title,
    int TargetMinutesPerWeek,
    int MinutesThisWeek,
    DateTime CreatedAtUtc);

