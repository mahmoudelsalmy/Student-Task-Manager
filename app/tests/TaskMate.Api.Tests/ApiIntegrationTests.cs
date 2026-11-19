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

    public ApiIntegrationTests(TaskMateApiFactory factory)
    {
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
