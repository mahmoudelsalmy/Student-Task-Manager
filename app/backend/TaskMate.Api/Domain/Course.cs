namespace TaskMate.Api.Domain;

public sealed class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Instructor { get; set; }
    public string? Semester { get; set; }
    public string Color { get; set; } = "#64748b";
    public DateTime CreatedAtUtc { get; set; }
}
