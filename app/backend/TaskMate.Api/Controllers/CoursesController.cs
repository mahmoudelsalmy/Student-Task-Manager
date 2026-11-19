using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskMate.Api.Contracts;
using TaskMate.Api.Data;
using TaskMate.Api.Domain;

namespace TaskMate.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/courses")]
public sealed class CoursesController(TaskMateDbContext db, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourseResponse>>> GetCourses(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var courses = await db.Courses.AsNoTracking()
            .Where(course => course.UserId == userId.Value)
            .OrderBy(course => course.Name)
            .Select(course => ToResponse(course))
            .ToListAsync(cancellationToken);
        return Ok(courses);
    }

    [HttpGet("{courseId:guid}")]
    public async Task<ActionResult<CourseResponse>> GetCourse(Guid courseId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var course = await db.Courses.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == courseId && item.UserId == userId.Value, cancellationToken);
        return course is null ? NotFound() : Ok(ToResponse(course));
    }

    [HttpPost]
    public async Task<ActionResult<CourseResponse>> CreateCourse(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var course = new Course
        {
            UserId = userId.Value,
            Name = request.Name.Trim(),
            Code = Normalize(request.Code),
            Instructor = Normalize(request.Instructor),
            Semester = Normalize(request.Semester),
            Color = request.Color,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetCourse), new { courseId = course.Id }, ToResponse(course));
    }

    [HttpPut("{courseId:guid}")]
    public async Task<ActionResult<CourseResponse>> UpdateCourse(Guid courseId, UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var course = await db.Courses.SingleOrDefaultAsync(
            item => item.Id == courseId && item.UserId == userId.Value, cancellationToken);
        if (course is null) return NotFound();
        course.Name = request.Name.Trim();
        course.Code = Normalize(request.Code);
        course.Instructor = Normalize(request.Instructor);
        course.Semester = Normalize(request.Semester);
        course.Color = request.Color;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(course));
    }

    [HttpDelete("{courseId:guid}")]
    public async Task<IActionResult> DeleteCourse(Guid courseId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var course = await db.Courses.SingleOrDefaultAsync(
            item => item.Id == courseId && item.UserId == userId.Value, cancellationToken);
        if (course is null) return NotFound();
        db.Courses.Remove(course);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CourseResponse ToResponse(Course course) =>
        new(course.Id, course.Name, course.Code, course.Instructor, course.Semester, course.Color, course.CreatedAtUtc);
}
