namespace TaskMate.Api.Domain;

public sealed class StudyGoal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TargetMinutesPerWeek { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

