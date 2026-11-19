namespace TaskMate.Api.Domain;
public sealed class StudySession
{
 public Guid Id { get; set; } = Guid.NewGuid();
 public Guid UserId { get; set; }
 public Guid? CourseId { get; set; }
 public Guid? TaskId { get; set; }
 public DateTime StartedAtUtc { get; set; }
 public DateTime EndedAtUtc { get; set; }
 public int DurationMinutes { get; set; }
 public string? Notes { get; set; }
 public DateTime CreatedAtUtc { get; set; }
}
