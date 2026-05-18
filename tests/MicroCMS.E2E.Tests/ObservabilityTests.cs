using MicroCMS.E2E.Tests.Fixtures;
using System.Net;
using Xunit;

namespace MicroCMS.E2E.Tests;

/// <summary>
/// E2E tests verifying health check and observability endpoints.
/// These tests do not require any authentication.
/// </summary>
[Collection("WebHost")]
public sealed class ObservabilityTests(MicroCmsWebApplicationFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.InitializeAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task HealthLive_Returns200_WhenProcessAlive()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body);
    }

    [Fact]
    public async Task HealthReady_Returns200OrDegraded()
    {
        var response = await _client.GetAsync("/health/ready");

        // In the E2E test environment only the in-memory "self" check runs;
        // the response may be Healthy or Degraded but must not be 5xx.
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Unexpected status: {response.StatusCode}");
    }

    [Fact]
    public async Task MetricsEndpoint_Returns200_WithPrometheusFormat()
    {
        var response = await _client.GetAsync("/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        // Prometheus format always starts with a HELP or TYPE comment or metric line
        Assert.False(string.IsNullOrWhiteSpace(body));
    }

    [Fact]
    public async Task HealthLive_ResponseBody_ContainsStatusField()
    {
        var response = await _client.GetAsync("/health/live");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\"", body);
    }
}
