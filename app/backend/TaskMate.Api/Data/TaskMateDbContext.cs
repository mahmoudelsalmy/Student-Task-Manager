using Microsoft.EntityFrameworkCore;
using TaskMate.Api.Domain;

namespace TaskMate.Api.Data;

public sealed class TaskMateDbContext(DbContextOptions<TaskMateDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<StudyGoal> StudyGoals => Set<StudyGoal>();
    public DbSet<StudyPlanBlock> StudyPlanBlocks => Set<StudyPlanBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Name).HasMaxLength(80).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(254).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(254).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(256).IsRequired();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.HasMany(user => user.Tasks)
                .WithOne(task => task.User)
                .HasForeignKey(task => task.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(course => course.Id);
            entity.Property(course => course.Name).HasMaxLength(100).IsRequired();
            entity.Property(course => course.Code).HasMaxLength(20);
            entity.Property(course => course.Instructor).HasMaxLength(100);
            entity.Property(course => course.Semester).HasMaxLength(40);
            entity.Property(course => course.Color).HasMaxLength(7).IsRequired();
            entity.HasIndex(course => new { course.UserId, course.Name });
            entity.HasIndex(course => new { course.UserId, course.Code }).IsUnique();
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(course => course.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudySession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasIndex(x => new { x.UserId, x.StartedAtUtc });
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasKey(exam => exam.Id);
            entity.Property(exam => exam.Title).HasMaxLength(120).IsRequired();
            entity.Property(exam => exam.Description).HasMaxLength(2000);
            entity.Property(exam => exam.Location).HasMaxLength(120);
            entity.HasIndex(exam => new { exam.UserId, exam.ScheduledAtUtc });
            entity.HasIndex(exam => exam.CourseId);
            entity.HasOne<User>().WithMany().HasForeignKey(exam => exam.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Course>().WithMany().HasForeignKey(exam => exam.CourseId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StudyGoal>(entity =>
        {
            entity.HasKey(goal => goal.Id);
            entity.Property(goal => goal.Title).HasMaxLength(100).IsRequired();
            entity.HasIndex(goal => goal.UserId);
            entity.HasIndex(goal => goal.CourseId);
            entity.HasOne<User>().WithMany().HasForeignKey(goal => goal.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Course>().WithMany().HasForeignKey(goal => goal.CourseId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StudyPlanBlock>(entity =>
        {
            entity.HasKey(block => block.Id);
            entity.Property(block => block.Title).HasMaxLength(120).IsRequired();
            entity.HasIndex(block => new { block.UserId, block.StartsAtUtc });
            entity.HasIndex(block => block.CourseId);
            entity.HasIndex(block => block.TaskId);
            entity.HasOne<User>().WithMany().HasForeignKey(block => block.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Course>().WithMany().HasForeignKey(block => block.CourseId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<TaskItem>().WithMany().HasForeignKey(block => block.TaskId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).HasMaxLength(120).IsRequired();
            entity.Property(task => task.Description).HasMaxLength(5000);
            entity.Property(task => task.Priority).HasConversion<string>().HasMaxLength(16);
            entity.Property(task => task.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(task => task.Category).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(task => new { task.UserId, task.DueDate });
            entity.HasIndex(task => new { task.UserId, task.Status });
            entity.HasIndex(task => task.CourseId);
            entity.HasOne(task => task.Course)
                .WithMany()
                .HasForeignKey(task => task.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
