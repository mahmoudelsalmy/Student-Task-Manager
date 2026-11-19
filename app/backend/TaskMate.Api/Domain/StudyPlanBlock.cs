namespace TaskMate.Api.Domain;

public sealed class StudyPlanBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? CourseId { get; set; }
    public Guid? TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
