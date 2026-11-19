using Microsoft.EntityFrameworkCore;
using TaskMate.Api.Domain;

namespace TaskMate.Api.Data;

public sealed class TaskMateDbContext(DbContextOptions<TaskMateDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Course> Courses => Set<Course>();

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
        });
    }
}
