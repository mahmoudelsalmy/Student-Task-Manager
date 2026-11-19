using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace TaskMate.Api.Tests;

public sealed class ApiIntegrationTests : IClassFixture<TaskMateApiFactory>
{
    private readonly HttpClient _client;
    private readonly TaskMateApiFactory _factory;

    public ApiIntegrationTests(TaskMateApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task TaskEndpoints_RequireAuthentication()
    {
        var response = await _client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthFlow_RegistersLogsInAndProtectsUserTasks()
    {
        var email = $"integration-{Guid.NewGuid():N}@example.com";
        const string password = "Strong-password-123";

        var register = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Integration User",
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        using var registeredBody = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        var token = registeredBody.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var duplicateRegister = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Integration User",
            email,
            password
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateRegister.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginBody = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var loginToken = loginBody.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(loginToken));

        var invalidLogin = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Wrong-password-123"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, invalidLogin.StatusCode);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginToken);

        var currentUser = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, currentUser.StatusCode);

        var tasks = await _client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.OK, tasks.StatusCode);
    }

    [Fact]
    public async Task Register_RejectsInvalidRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = " ",
            email = "not-an-email",
            password = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task Tasks_AreCreatedAndVisibleOnlyToTheirOwner()
    {
        var ownerClient = _client;
        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var ownerToken = await RegisterAndGetToken(ownerClient, ownerEmail);
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var create = await ownerClient.PostAsJsonAsync("/api/tasks", new
        {
            title = "Integration test assignment",
            description = "Verify owner isolation",
            dueDate = "2026-12-01",
            priority = "High",
            status = "ToDo",
            category = "Assignment"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createdBody = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var taskId = createdBody.RootElement.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(taskId));

        var ownerRead = await ownerClient.GetAsync($"/api/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.OK, ownerRead.StatusCode);

        using var otherClient = _factory.CreateClient();
        var otherToken = await RegisterAndGetToken(otherClient, $"other-{Guid.NewGuid():N}@example.com");
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var otherRead = await otherClient.GetAsync($"/api/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.NotFound, otherRead.StatusCode);

        var otherUpdate = await otherClient.PutAsJsonAsync($"/api/tasks/{taskId}", new
        {
            title = "Attempted takeover",
            description = (string?)null,
            dueDate = (string?)null,
            priority = "Low",
            status = "Completed",
            category = "Personal"
        });
        Assert.Equal(HttpStatusCode.NotFound, otherUpdate.StatusCode);

        var ownerDelete = await ownerClient.DeleteAsync($"/api/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.NoContent, ownerDelete.StatusCode);
    }

    [Fact]
    public async Task Dashboard_RejectsDatesOutsideExpectedFormat()
    {
        var token = await RegisterAndGetToken(_client, $"dashboard-{Guid.NewGuid():N}@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/dashboard?today=12-01-2026");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<string> RegisterAndGetToken(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Integration Test User",
            email,
            password = "Strong-password-123"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Registration did not return an access token.");
    }

    [Fact]
    public async Task Courses_AreOwnedByTheCreatingUser()
    {
        var ownerToken = await RegisterAndGetToken(_client, $"course-owner-{Guid.NewGuid():N}@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var create = await _client.PostAsJsonAsync("/api/courses", new
        {
            name = "Digital Control Systems",
            code = "CSE401",
            instructor = "Dr. Example",
            semester = "Fall 2026",
            color = "#2563eb"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var courseId = created.RootElement.GetProperty("id").GetString();
        Assert.Equal("CSE401", created.RootElement.GetProperty("code").GetString());

        var list = await _client.GetAsync("/api/courses");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listBody = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Contains(listBody.RootElement.EnumerateArray(),
            course => course.GetProperty("id").GetString() == courseId);

        using var otherClient = _factory.CreateClient();
        var otherToken = await RegisterAndGetToken(otherClient, $"course-other-{Guid.NewGuid():N}@example.com");
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var otherRead = await otherClient.GetAsync($"/api/courses/{courseId}");
        Assert.Equal(HttpStatusCode.NotFound, otherRead.StatusCode);

        var ownerDelete = await _client.DeleteAsync($"/api/courses/{courseId}");
        Assert.Equal(HttpStatusCode.NoContent, ownerDelete.StatusCode);
    }

    [Fact]
    public async Task Courses_RejectInvalidColor()
    {
        var token = await RegisterAndGetToken(_client, $"course-invalid-{Guid.NewGuid():N}@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/courses", new
        {
            name = "Valid course name",
            color = "not-a-color"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Tasks_CanLinkToOwnedCourse_RejectForeignCourse_AndClearLinkOnCourseDelete()
    {
        using var ownerClient = _factory.CreateClient();
        var ownerToken = await RegisterAndGetToken(ownerClient, $"task-course-owner-{Guid.NewGuid():N}@example.com");
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var courseResponse = await ownerClient.PostAsJsonAsync("/api/courses", new { name = "Linear Algebra" });
        Assert.Equal(HttpStatusCode.Created, courseResponse.StatusCode);
        using var courseBody = JsonDocument.Parse(await courseResponse.Content.ReadAsStringAsync());
        var courseId = courseBody.RootElement.GetProperty("id").GetGuid();

        var create = await ownerClient.PostAsJsonAsync("/api/tasks", new
        {
            title = "Read matrix notes",
            category = "Assignment",
            courseId
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var taskBody = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var taskId = taskBody.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(courseId, taskBody.RootElement.GetProperty("courseId").GetGuid());

        using var otherClient = _factory.CreateClient();
        var otherToken = await RegisterAndGetToken(otherClient, $"task-course-other-{Guid.NewGuid():N}@example.com");
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var otherCourseResponse = await otherClient.PostAsJsonAsync("/api/courses", new { name = "Other user's course" });
        Assert.Equal(HttpStatusCode.Created, otherCourseResponse.StatusCode);
        using var otherCourseBody = JsonDocument.Parse(await otherCourseResponse.Content.ReadAsStringAsync());
        var otherCourseId = otherCourseBody.RootElement.GetProperty("id").GetGuid();
        var invalidCreate = await otherClient.PostAsJsonAsync("/api/tasks", new { title = "Foreign course", courseId });
        Assert.Equal(HttpStatusCode.BadRequest, invalidCreate.StatusCode);

        var validUpdate = await ownerClient.PutAsJsonAsync($"/api/tasks/{taskId}", new
        {
            title = "Read matrix notes, section two", description = (string?)null, dueDate = (string?)null,
            priority = "Medium", status = "ToDo", category = "Assignment", courseId
        });
        Assert.Equal(HttpStatusCode.OK, validUpdate.StatusCode);
        using var updatedBody = JsonDocument.Parse(await validUpdate.Content.ReadAsStringAsync());
        Assert.Equal(courseId, updatedBody.RootElement.GetProperty("courseId").GetGuid());

        var invalidUpdate = await ownerClient.PutAsJsonAsync($"/api/tasks/{taskId}", new
        {
            title = "Read matrix notes", description = (string?)null, dueDate = (string?)null,
            priority = "Medium", status = "ToDo", category = "Assignment", courseId = otherCourseId
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidUpdate.StatusCode);

        var deleteCourse = await ownerClient.DeleteAsync($"/api/courses/{courseId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCourse.StatusCode);
        var readTask = await ownerClient.GetAsync($"/api/tasks/{taskId}");
        using var updatedTask = JsonDocument.Parse(await readTask.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, updatedTask.RootElement.GetProperty("courseId").ValueKind);
    }

    [Fact]
    public async Task StudySessions_AreValidatedOwnerScopedAndIncludedInSummaries()
    {
        using var ownerClient = _factory.CreateClient();
        var ownerToken = await RegisterAndGetToken(ownerClient, $"study-owner-{Guid.NewGuid():N}@example.com");
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var courseResponse = await ownerClient.PostAsJsonAsync("/api/courses", new { name = "Study Systems" });
        Assert.Equal(HttpStatusCode.Created, courseResponse.StatusCode);
        using var courseBody = JsonDocument.Parse(await courseResponse.Content.ReadAsStringAsync());
        var courseId = courseBody.RootElement.GetProperty("id").GetGuid();
        var taskResponse = await ownerClient.PostAsJsonAsync("/api/tasks", new { title = "Review lecture notes", courseId });
        Assert.Equal(HttpStatusCode.Created, taskResponse.StatusCode);
        using var taskBody = JsonDocument.Parse(await taskResponse.Content.ReadAsStringAsync());
        var taskId = taskBody.RootElement.GetProperty("id").GetGuid();

        var end = DateTimeOffset.UtcNow.AddMinutes(-2);
        var start = end.AddMinutes(-28);
        var create = await ownerClient.PostAsJsonAsync("/api/study/sessions", new
        {
            startedAtUtc = start,
            endedAtUtc = end,
            actualFocusSeconds = 25 * 60,
            courseId,
            taskId,
            notes = "Reviewed the lecture examples"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        Assert.Equal(25, created.RootElement.GetProperty("durationMinutes").GetInt32());
        Assert.Equal(courseId, created.RootElement.GetProperty("courseId").GetGuid());
        Assert.Equal(taskId, created.RootElement.GetProperty("taskId").GetGuid());

        var history = await ownerClient.GetAsync("/api/study/sessions");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var historyBody = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        Assert.Single(historyBody.RootElement.EnumerateArray());
        var summaryResponse = await ownerClient.GetAsync("/api/study/summary");
        using var summary = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, summary.RootElement.GetProperty("sessions").GetInt32());
        Assert.Equal(25, summary.RootElement.GetProperty("totalMinutes").GetInt32());

        using var otherClient = _factory.CreateClient();
        var otherToken = await RegisterAndGetToken(otherClient, $"study-other-{Guid.NewGuid():N}@example.com");
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var otherHistory = await otherClient.GetAsync("/api/study/sessions");
        using var otherHistoryBody = JsonDocument.Parse(await otherHistory.Content.ReadAsStringAsync());
        Assert.Empty(otherHistoryBody.RootElement.EnumerateArray());
        var foreignReference = await otherClient.PostAsJsonAsync("/api/study/sessions", new
        {
            startedAtUtc = start, endedAtUtc = end, actualFocusSeconds = 25 * 60, courseId, taskId
        });
        Assert.Equal(HttpStatusCode.BadRequest, foreignReference.StatusCode);

        var tooMuchFocus = await ownerClient.PostAsJsonAsync("/api/study/sessions", new
        {
            startedAtUtc = end.AddMinutes(-1), endedAtUtc = end, actualFocusSeconds = 10 * 60
        });
        Assert.Equal(HttpStatusCode.BadRequest, tooMuchFocus.StatusCode);
        var futureSession = await ownerClient.PostAsJsonAsync("/api/study/sessions", new
        {
            startedAtUtc = DateTimeOffset.UtcNow, endedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1), actualFocusSeconds = 60
        });
        Assert.Equal(HttpStatusCode.BadRequest, futureSession.StatusCode);
        var nonUtcRequest = await ownerClient.PostAsJsonAsync("/api/study/sessions", new
        {
            startedAtUtc = start.ToOffset(TimeSpan.FromHours(2)),
            endedAtUtc = end.ToOffset(TimeSpan.FromHours(2)),
            actualFocusSeconds = 25 * 60
        });
        Assert.Equal(HttpStatusCode.BadRequest, nonUtcRequest.StatusCode);
    }

    [Fact]
    public async Task StudyEndpoints_RequireAuthentication()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/study/sessions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/study/summary")).StatusCode);
    }

    [Fact]
    public async Task Exams_AreCrudOwnerScopedAndValidateCourseAndUtc()
    {
        using var owner = _factory.CreateClient();
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await RegisterAndGetToken(owner, $"exam-owner-{Guid.NewGuid():N}@example.com"));
        var courseResponse = await owner.PostAsJsonAsync("/api/courses", new { name = "Chemistry" });
        using var courseJson = JsonDocument.Parse(await courseResponse.Content.ReadAsStringAsync());
        var courseId = courseJson.RootElement.GetProperty("id").GetGuid();
        var scheduled = DateTimeOffset.UtcNow.AddDays(9);
        var create = await owner.PostAsJsonAsync("/api/exams", new
        {
            title = "Organic chemistry midterm",
            description = "Chapters 1 through 5",
            scheduledAtUtc = scheduled,
            durationMinutes = 120,
            courseId,
            location = "Science Hall 204"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var examId = created.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(courseId, created.RootElement.GetProperty("courseId").GetGuid());
        Assert.Equal("Science Hall 204", created.RootElement.GetProperty("location").GetString());

        var list = await owner.GetAsync("/api/exams");
        using var listed = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Single(listed.RootElement.EnumerateArray());
        var update = await owner.PutAsJsonAsync($"/api/exams/{examId}", new
        {
            title = "Organic chemistry final",
            description = (string?)null,
            scheduledAtUtc = scheduled.AddDays(2),
            durationMinutes = 180,
            courseId = (Guid?)null,
            location = "Hall 2"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var wrongOffset = await owner.PostAsJsonAsync("/api/exams", new
        {
            title = "Invalid offset exam",
            scheduledAtUtc = scheduled.ToOffset(TimeSpan.FromHours(2)),
            durationMinutes = 90
        });
        Assert.Equal(HttpStatusCode.BadRequest, wrongOffset.StatusCode);

        using var other = _factory.CreateClient();
        other.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await RegisterAndGetToken(other, $"exam-other-{Guid.NewGuid():N}@example.com"));
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/exams/{examId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/exams/{examId}")).StatusCode);
        var foreignCourse = await other.PostAsJsonAsync("/api/exams", new
        {
            title = "Foreign course exam", scheduledAtUtc = scheduled, courseId
        });
        Assert.Equal(HttpStatusCode.BadRequest, foreignCourse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/exams/{examId}")).StatusCode);
    }

    [Fact]
    public async Task StudyGoals_TrackWeeklyOwnerSessionProgressAndValidateCourseOwnership()
    {
        using var owner = _factory.CreateClient();
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await RegisterAndGetToken(owner, $"goal-owner-{Guid.NewGuid():N}@example.com"));
        var courseResponse = await owner.PostAsJsonAsync("/api/courses", new { name = "Physics" });
        using var courseJson = JsonDocument.Parse(await courseResponse.Content.ReadAsStringAsync());
        var courseId = courseJson.RootElement.GetProperty("id").GetGuid();
        var end = DateTimeOffset.UtcNow.AddMinutes(-1);
        await owner.PostAsJsonAsync("/api/study/sessions", new
        {
            startedAtUtc = end.AddMinutes(-15),
            endedAtUtc = end,
            actualFocusSeconds = 10 * 60,
            courseId
        });
        var create = await owner.PostAsJsonAsync("/api/study/goals", new
        {
            title = "Physics review", targetMinutesPerWeek = 300, courseId
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var goalId = created.RootElement.GetProperty("id").GetGuid();
        var list = await owner.GetAsync("/api/study/goals");
        using var goals = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Single(goals.RootElement.EnumerateArray());
        Assert.Equal(10, goals.RootElement[0].GetProperty("minutesThisWeek").GetInt32());

        using var other = _factory.CreateClient();
        other.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await RegisterAndGetToken(other, $"goal-other-{Guid.NewGuid():N}@example.com"));
        var hidden = await other.GetAsync("/api/study/goals");
        using var otherGoals = JsonDocument.Parse(await hidden.Content.ReadAsStringAsync());
        Assert.Empty(otherGoals.RootElement.EnumerateArray());
        var foreign = await other.PostAsJsonAsync("/api/study/goals", new
        {
            title = "Foreign course", targetMinutesPerWeek = 90, courseId
        });
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);

        var update = await owner.PutAsJsonAsync($"/api/study/goals/{goalId}", new
        {
            title = "Physics deep work", targetMinutesPerWeek = 360, courseId = (Guid?)null
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/study/goals/{goalId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/study/goals/{goalId}")).StatusCode);
    }

}

public sealed class TaskMateApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"taskmate-integration-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_databasePath};Cache=Shared;Foreign Keys=True",
                ["Authentication:Jwt:Issuer"] = "TaskMate.Tests",
                ["Authentication:Jwt:Audience"] = "TaskMate.Tests",
                ["Authentication:Jwt:SigningKey"] = "integration-test-signing-key-at-least-32-bytes-long"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}

