using MicroCMS.E2E.Tests.Fixtures;
using System.Net;
using Xunit;

namespace MicroCMS.E2E.Tests;

/// <summary>
/// E2E tests for core content APIs — verifies routes resolve and auth is enforced.
/// Full CRUD round-trips require a seeded database; these tests cover reachability
/// and guard behaviour on the happy path and key error paths.
/// </summary>
[Collection("WebHost")]
public sealed class ContentApiTests(MicroCmsWebApplicationFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _anonClient = factory.CreateClient();
    private readonly HttpClient _authClient = factory.CreateAuthenticatedClient();

    public Task InitializeAsync() => factory.InitializeAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Content types ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetContentTypes_Authenticated_Returns200Or404()
    {
        var response = await _authClient.GetAsync("/api/v1/content-types");

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"Unexpected: {response.StatusCode}");
    }

    [Fact]
    public async Task GetContentTypes_Unauthenticated_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/content-types");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Entries ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEntries_Authenticated_Returns200Or404()
    {
        var response = await _authClient.GetAsync("/api/v1/entries");

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"Unexpected: {response.StatusCode}");
    }

    [Fact]
    public async Task GetEntries_Unauthenticated_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/entries");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Media ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMedia_Unauthenticated_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/media");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Taxonomy ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTaxonomy_Unauthenticated_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/taxonomy/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Search ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchEndpoint_Unauthenticated_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/search?q=test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Unknown routes ────────────────────────────────────────────────────

    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        var response = await _authClient.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
