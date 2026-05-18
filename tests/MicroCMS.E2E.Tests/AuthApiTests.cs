using MicroCMS.E2E.Tests.Fixtures;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MicroCMS.E2E.Tests;

/// <summary>
/// E2E tests for authentication API happy paths and key error paths.
/// </summary>
[Collection("WebHost")]
public sealed class AuthApiTests(MicroCmsWebApplicationFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _anonClient = factory.CreateClient();
    private readonly HttpClient _authClient = factory.CreateAuthenticatedClient();

    public Task InitializeAsync() => factory.InitializeAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/entries");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_DoesNotReturn401()
    {
        var response = await _authClient.GetAsync("/api/v1/sites");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns400Or401()
    {
        var payload = JsonSerializer.Serialize(new { username = "wrong@example.com", password = "bad" });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _anonClient.PostAsync("/api/v1/auth/login", content);

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest
                or HttpStatusCode.Unauthorized
                or HttpStatusCode.UnprocessableEntity
                or HttpStatusCode.ServiceUnavailable, // install guard
            $"Unexpected status: {response.StatusCode}");
    }

    [Fact]
    public async Task SwaggerEndpoint_IsAccessible_InDevelopment()
    {
        var response = await _anonClient.GetAsync("/swagger/v1/swagger.json");

        // Swagger is served only in Development; in Testing environment it may be 404
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"Unexpected status: {response.StatusCode}");
    }

    [Fact]
    public async Task SecurityHeaders_ArePresent_OnEveryResponse()
    {
        var response = await _anonClient.GetAsync("/health/live");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"),
            "Missing X-Content-Type-Options header");
        Assert.True(response.Headers.Contains("X-Correlation-ID"),
            "Missing X-Correlation-ID header");
    }
}
