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
        const string email = "integration@example.com";
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
