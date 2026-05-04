using FluentAssertions;
using MicroCMS.Api.ContractTests.Fixtures;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Shared.Ids;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MicroCMS.Api.ContractTests;

// ── Shared seeding helper ──────────────────────────────────────────────────────

file static class AiTestSeed
{
    /// <summary>
    /// Inserts a minimal ContentType and Entry into the in-memory database and
    /// returns their IDs for use in AI endpoint tests.
    /// </summary>
    public static async Task<(Guid tenantId, Guid siteId, Guid contentTypeId, Guid entryId)> SeedEntryAsync(
        IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenantId = ApiWebApplicationFactory.TestTenantId;
        var siteId = SiteId.New();
        var contentTypeId = ContentTypeId.New();

        var contentType = ContentType.Create(
            tenantId, siteId, "blog-post", "Blog Post");

        var entry = Entry.Create(
            tenantId, siteId, contentTypeId,
            Slug.Create("test-entry"),
            Locale.English,
            ApiWebApplicationFactory.TestUserId,
            fieldsJson: """{"title":"Test Entry","body":"Some content here."}""");

        db.Set<ContentType>().Add(contentType);
        db.Set<Entry>().Add(entry);
        await db.SaveChangesAsync();

        return (tenantId.Value, siteId.Value, contentType.Id.Value, entry.Id.Value);
    }

    /// <summary>
    /// Inserts a minimal available image MediaAsset and returns its ID.
    /// </summary>
    public static async Task<Guid> SeedImageAssetAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenantId = ApiWebApplicationFactory.TestTenantId;
        var siteId = SiteId.New();

        var metadata = AssetMetadata.Create(
            fileName: "hero.jpg",
            mimeType: "image/jpeg",
            sizeBytes: 204_800,
            widthPx: 1920,
            heightPx: 1080);

        var asset = MediaAsset.Create(
            tenantId, siteId, metadata,
            storageKey: "uploads/hero.jpg",
            uploadedBy: ApiWebApplicationFactory.TestUserId);

        asset.MarkUploadComplete();
        asset.MarkAvailable();

        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync();

        return asset.Id.Value;
    }
}

// ── Factory: stubs ILlmService with canned responses ──────────────────────────

/// <summary>
/// Test factory that replaces <see cref="ILlmService"/> with a configurable stub,
/// allowing contract tests to assert on AI endpoint shapes without a real LLM.
/// </summary>
public sealed class AiStubbedLlmFactory : ApiWebApplicationFactory
{
    public ILlmService LlmStub { get; } = Substitute.For<ILlmService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            // Replace NullLlmService with our configurable stub
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ILlmService));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddScoped<ILlmService>(_ => LlmStub);
        });
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 1.  AUTH GUARD TESTS — all AI endpoints must return 401 without a token
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Verifies that every AI endpoint is covered by [Authorize] and returns 401
/// when called without a Bearer token.
/// </summary>
public sealed class AiEndpointAuthTests
{
    private static readonly Guid AnyGuid = Guid.NewGuid();

    [Theory]
    [InlineData("POST", "/api/v1/ai/drafts/generate")]
    [InlineData("POST", "/api/v1/ai-writing/{id}/draft")]
    [InlineData("POST", "/api/v1/ai-writing/{id}/rewrite")]
    [InlineData("POST", "/api/v1/ai-writing/{id}/tone")]
    [InlineData("GET",  "/api/v1/ai-writing/{id}/summarize")]
    [InlineData("POST", "/api/v1/ai-writing/{id}/translate")]
    [InlineData("GET",  "/api/v1/entries/{id}/quality-checks")]
    [InlineData("POST", "/api/v1/media/{id}/alt-text")]
    public async Task AiEndpoint_WithoutToken_Returns401(string method, string pathTemplate)
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        // Remove the default auth header injected by the factory's stub
        client.DefaultRequestHeaders.Remove("Authorization");

        var path = pathTemplate.Replace("{id}", AnyGuid.ToString());
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST")
            request.Content = JsonContent.Create(new { });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: $"{method} {path} must require authentication");
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 2.  GENERATE DRAFT — POST /api/v1/ai/drafts/generate
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class GenerateDraftContractTests : IAsyncLifetime
{
    private readonly AiStubbedLlmFactory _factory = new();
    private HttpClient _client = null!;
    private Guid _siteId;
    private Guid _contentTypeId;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        var seeded = await AiTestSeed.SeedEntryAsync(_factory.Services);
        _siteId = seeded.siteId;
        _contentTypeId = seeded.contentTypeId;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task GenerateDraft_WhenPromptsNotConfigured_Returns422()
    {
        // AI prompts are not seeded in config — handler returns Validation failure
        var response = await _client.PostAsJsonAsync("/api/v1/ai/drafts/generate", new
        {
            siteId = _siteId,
            contentTypeId = _contentTypeId,
            prompt = "Write a blog post about .NET 8",
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            because: "the handler returns a Validation error when AI prompts are not configured");
    }

    [Fact]
    public async Task GenerateDraft_WithUnknownContentType_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/ai/drafts/generate", new
        {
            siteId = _siteId,
            contentTypeId = Guid.NewGuid(), // does not exist
            prompt = "Write a blog post",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            because: "the handler returns NotFound when the content type does not exist");
    }

    [Fact]
    public async Task GenerateDraft_WithMissingRequiredFields_Returns400()
    {
        // Empty prompt — required field
        var response = await _client.PostAsJsonAsync("/api/v1/ai/drafts/generate", new
        {
            siteId = _siteId,
            contentTypeId = _contentTypeId,
            // prompt is missing
        });

        // ASP.NET Core model binding / validation rejects before handler is reached
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 3.  AI WRITING ASSIST — POST /api/v1/ai-writing/{entryId}/...
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class AiWritingContractTests : IAsyncLifetime
{
    private readonly AiStubbedLlmFactory _factory = new();
    private HttpClient _client = null!;
    private Guid _entryId;

    private static readonly LlmResponse CannedResponse = new(
        Content: "This is AI-generated content.",
        PromptTokens: 42,
        CompletionTokens: 18,
        ProviderName: "stub",
        Model: "stub-1.0");

    public async Task InitializeAsync()
    {
        // Wire the LLM stub to return a canned response for any call
        _factory.LlmStub
            .CompleteAsync(Arg.Any<LlmRequest>(), Arg.Any<SiteId?>(), Arg.Any<CancellationToken>())
            .Returns(CannedResponse);

        _client = _factory.CreateClient();
        var seeded = await AiTestSeed.SeedEntryAsync(_factory.Services);
        _entryId = seeded.entryId;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // ── Draft ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Draft_WithValidEntryAndPrompt_Returns200WithGeneratedText()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/ai-writing/{_entryId}/draft",
            new { prompt = "Make the title catchy" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AiContentResultShape>();
        body.Should().NotBeNull();
        body!.GeneratedText.Should().Be("This is AI-generated content.");
        body.ProviderName.Should().Be("stub");
        body.PromptTokens.Should().Be(42);
        body.CompletionTokens.Should().Be(18);
    }

    [Fact]
    public async Task Draft_WithUnknownEntryId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/ai-writing/{Guid.NewGuid()}/draft",
            new { prompt = "Write something" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Rewrite ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Rewrite_WithValidPayload_Returns200WithGeneratedText()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/ai-writing/{_entryId}/rewrite",
            new { fieldHandle = "body", instructions = "Make it shorter and punchier" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AiContentResultShape>();
        body!.GeneratedText.Should().Be("This is AI-generated content.");
    }

    [Fact]
    public async Task Rewrite_WithUnknownEntryId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/ai-writing/{Guid.NewGuid()}/rewrite",
            new { fieldHandle = "body", instructions = "Shorter" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Tone ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeTone_WithValidTone_Returns200()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/ai-writing/{_entryId}/tone",
            new { fieldHandle = "body", tone = "Casual" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AiContentResultShape>();
        body!.GeneratedText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangeTone_WithUnknownEntryId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/ai-writing/{Guid.NewGuid()}/tone",
            new { fieldHandle = "body", tone = "Professional" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Summarize ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Summarize_WithValidEntry_Returns200WithGeneratedText()
    {
        var response = await _client.GetAsync(
            $"/api/v1/ai-writing/{_entryId}/summarize?fieldHandle=body&maxSentences=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AiContentResultShape>();
        body!.GeneratedText.Should().Be("This is AI-generated content.");
    }

    [Fact]
    public async Task Summarize_WithUnknownEntryId_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/v1/ai-writing/{Guid.NewGuid()}/summarize?fieldHandle=body");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Translate ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Translate_WithValidLocales_Returns200()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/ai-writing/{_entryId}/translate",
            new { sourceLocale = "en", targetLocale = "de" });

        // Translation returns an EntryDto on success
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Translate_WithUnknownEntryId_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/ai-writing/{Guid.NewGuid()}/translate",
            new { sourceLocale = "en", targetLocale = "fr" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 4.  QUALITY CHECKS — GET /api/v1/entries/{entryId}/quality-checks
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class QualityChecksContractTests : IAsyncLifetime
{
    private readonly AiStubbedLlmFactory _factory = new();
    private HttpClient _client = null!;
    private Guid _entryId;

    private static readonly LlmResponse QualityCheckResponse = new(
        Content: """{"grammarScore":0.92,"readabilityGrade":"B","suggestions":["Consider shorter sentences."]}""",
        PromptTokens: 30,
        CompletionTokens: 20,
        ProviderName: "stub",
        Model: "stub-1.0");

    public async Task InitializeAsync()
    {
        _factory.LlmStub
            .CompleteAsync(Arg.Any<LlmRequest>(), Arg.Any<SiteId?>(), Arg.Any<CancellationToken>())
            .Returns(QualityCheckResponse);

        _client = _factory.CreateClient();
        var seeded = await AiTestSeed.SeedEntryAsync(_factory.Services);
        _entryId = seeded.entryId;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task QualityChecks_WithValidEntry_Returns200WithReport()
    {
        var response = await _client.GetAsync($"/api/v1/entries/{_entryId}/quality-checks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<QualityCheckReportShape>();
        body.Should().NotBeNull();
        body!.EntryId.Should().Be(_entryId);
        body.GrammarScore.Should().BeGreaterThan(0);
        body.ReadabilityGrade.Should().NotBeNullOrEmpty();
        body.Suggestions.Should().NotBeNull();
    }

    [Fact]
    public async Task QualityChecks_WithUnknownEntryId_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/entries/{Guid.NewGuid()}/quality-checks");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task QualityChecks_PiiDetection_IsIncludedInReport()
    {
        // The handler always runs the PII regex scan regardless of LLM; verify the field exists
        var response = await _client.GetAsync($"/api/v1/entries/{_entryId}/quality-checks");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<QualityCheckReportShape>();
        body!.PiiDetected.Should().BeFalse(because: "test entry content contains no PII");
        body.PiiMatches.Should().BeEmpty();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 5.  AI ALT TEXT — POST /api/v1/media/{assetId}/alt-text
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class AiAltTextContractTests : IAsyncLifetime
{
    private readonly AiStubbedLlmFactory _factory = new();
    private HttpClient _client = null!;
    private Guid _assetId;

    private static readonly LlmResponse AltTextResponse = new(
        Content: "A wide hero image showing a laptop on a wooden desk.",
        PromptTokens: 15,
        CompletionTokens: 12,
        ProviderName: "stub",
        Model: "stub-vision-1.0");

    public async Task InitializeAsync()
    {
        _factory.LlmStub
            .CompleteAsync(Arg.Any<LlmRequest>(), Arg.Any<SiteId?>(), Arg.Any<CancellationToken>())
            .Returns(AltTextResponse);

        _client = _factory.CreateClient();
        _assetId = await AiTestSeed.SeedImageAssetAsync(_factory.Services);
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task GenerateAltText_WithAvailableImage_Returns200WithUpdatedAsset()
    {
        var response = await _client.PostAsync($"/api/v1/media/{_assetId}/alt-text", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<MediaAssetShape>();
        body.Should().NotBeNull();
        body!.AltText.Should().Be("A wide hero image showing a laptop on a wooden desk.",
            because: "the handler trims and stores the LLM response as alt text");
    }

    [Fact]
    public async Task GenerateAltText_WithUnknownAssetId_Returns404()
    {
        var response = await _client.PostAsync($"/api/v1/media/{Guid.NewGuid()}/alt-text", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateAltText_CallsLlmServiceExactlyOnce()
    {
        await _client.PostAsync($"/api/v1/media/{_assetId}/alt-text", null);

        await _factory.LlmStub
            .Received(1)
            .CompleteAsync(
                Arg.Is<LlmRequest>(r => r.FeatureHint == "alt_text"),
                Arg.Any<SiteId?>(),
                Arg.Any<CancellationToken>());
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// 6.  RESPONSE SHAPE RECORDS — lightweight deserialization targets
// ═══════════════════════════════════════════════════════════════════════════════

// These mirror the server-side DTOs without importing the Application assembly,
// keeping contract tests decoupled from internal types.

file sealed record AiContentResultShape(
    string GeneratedText,
    int PromptTokens,
    int CompletionTokens,
    string ProviderName);

file sealed record QualityCheckReportShape(
    Guid EntryId,
    double GrammarScore,
    string ReadabilityGrade,
    bool PiiDetected,
    List<object> PiiMatches,
    List<string> Suggestions);

file sealed record MediaAssetShape(
    Guid Id,
    string? AltText,
    string Status);
