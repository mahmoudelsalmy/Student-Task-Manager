using Xunit;
using TaskMate.Api.Contracts;
using TaskMate.Api.Domain;
using TaskMate.Api.Repositories;
using TaskMate.Api.Services;

namespace TaskMate.Api.Tests;

public sealed class TaskServiceTests
{
    private static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset FixedNow = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateTask_TrimsText_AndSetsOwnerAndTimestamps()
    {
        var repository = new FakeTaskRepository();
        var service = new TaskService(repository, new FixedTimeProvider(FixedNow));

        var result = await service.CreateTaskAsync(
            OwnerId,
            new CreateTaskRequest("  Study algorithms  ", "  Review graph traversal  ", new DateOnly(2026, 11, 25)),
            CancellationToken.None);

        var saved = Assert.Single(repository.Items);
        Assert.Equal(OwnerId, saved.UserId);
        Assert.Equal("Study algorithms", result.Title);
        Assert.Equal("Review graph traversal", result.Description);
        Assert.Equal(FixedNow.UtcDateTime, result.CreatedAtUtc);
        Assert.Equal(FixedNow.UtcDateTime, result.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetTask_DoesNotReturnAnotherUsersTask()
    {
        var repository = new FakeTaskRepository();
        var task = new TaskItem { UserId = OtherUserId, Title = "Private task" };
        repository.Items.Add(task);
        var service = new TaskService(repository, new FixedTimeProvider(FixedNow));

        var result = await service.GetTaskAsync(OwnerId, task.Id, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateTask_ReturnsNullForTaskOwnedByAnotherUser()
    {
        var repository = new FakeTaskRepository();
        var task = new TaskItem { UserId = OtherUserId, Title = "Private task" };
        repository.Items.Add(task);
        var service = new TaskService(repository, new FixedTimeProvider(FixedNow));

        var result = await service.UpdateTaskAsync(
            OwnerId,
            task.Id,
            new UpdateTaskRequest("Changed title", null, null, TaskPriority.High, TaskState.Completed, TaskCategory.Exam),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal("Private task", task.Title);
        Assert.Equal(TaskState.ToDo, task.Status);
    }

    [Fact]
    public async Task DeleteTask_OnlyDeletesTaskOwnedByRequestingUser()
    {
        var repository = new FakeTaskRepository();
        var owned = new TaskItem { UserId = OwnerId, Title = "My task" };
        var other = new TaskItem { UserId = OtherUserId, Title = "Not my task" };
        repository.Items.AddRange([owned, other]);
        var service = new TaskService(repository, new FixedTimeProvider(FixedNow));

        var deleted = await service.DeleteTaskAsync(OwnerId, owned.Id, CancellationToken.None);
        var denied = await service.DeleteTaskAsync(OwnerId, other.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.False(denied);
        Assert.Equal(other.Id, Assert.Single(repository.Items).Id);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        public List<TaskItem> Items { get; } = [];

        public Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> FindPageAsync(
            Guid userId, TaskQuery query, CancellationToken cancellationToken)
        {
            IReadOnlyList<TaskItem> items = Items.Where(item => item.UserId == userId).ToArray();
            return Task.FromResult((items, items.Count));
        }

        public Task<TaskItem?> FindByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == taskId && item.UserId == userId));

        public Task<bool> CourseBelongsToUserAsync(Guid userId, Guid courseId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
        {
            Items.Add(task);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
        {
            Items.Remove(task);
            return Task.CompletedTask;
        }

        public Task<DashboardResponse> GetDashboardAsync(Guid userId, DateOnly today, CancellationToken cancellationToken) =>
            Task.FromResult(new DashboardResponse(today, 0, 0, 0, 0, 0));
    }
}
