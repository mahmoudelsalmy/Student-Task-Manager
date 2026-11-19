namespace TaskMate.Api.Domain;

public sealed class Exam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 120;
    public string? Location { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

