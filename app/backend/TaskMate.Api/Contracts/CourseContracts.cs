using System.ComponentModel.DataAnnotations;

namespace TaskMate.Api.Contracts;

public sealed record CreateCourseRequest(
    [Required, StringLength(100, MinimumLength = 2), RegularExpression(@".*\S.*")] string Name,
    [StringLength(20)] string? Code = null,
    [StringLength(100)] string? Instructor = null,
    [StringLength(40)] string? Semester = null,
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")] string Color = "#64748b");

public sealed record UpdateCourseRequest(
    [Required, StringLength(100, MinimumLength = 2), RegularExpression(@".*\S.*")] string Name,
    [StringLength(20)] string? Code,
    [StringLength(100)] string? Instructor,
    [StringLength(40)] string? Semester,
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$")] string Color);

public sealed record CourseResponse(
    Guid Id,
    string Name,
    string? Code,
    string? Instructor,
    string? Semester,
    string Color,
    DateTime CreatedAtUtc);
