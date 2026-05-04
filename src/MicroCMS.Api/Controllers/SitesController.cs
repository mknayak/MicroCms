using Asp.Versioning;
using MicroCMS.Application.Features.Sites.Commands;
using MicroCMS.Application.Features.Sites.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Per-site management: general properties, feature-flag settings, and config entries.
/// Route: /api/v1/sites
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/sites")]
[ApiVersion("1.0")]
[Authorize]
public sealed class SitesController : ApiControllerBase
{
    /// <summary>Returns the detail of a single site (name, handle, locale, domain, environments).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SiteDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new GetSiteQuery(id), ct));

    /// <summary>Updates mutable site properties: name, default locale, and custom domain.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SiteDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSiteRequest request,
        CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(
            new UpdateSiteCommand(id, request.Name, request.DefaultLocale, request.CustomDomain), ct));

    /// <summary>Returns the feature-flag and delivery settings for a site.</summary>
    [HttpGet("{id:guid}/settings")]
    [ProducesResponseType(typeof(SiteSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSettings(Guid id, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new GetSiteSettingsQuery(id), ct));

    /// <summary>Updates feature flags, preview URL, CORS origins, and supported locales for a site.</summary>
    [HttpPut("{id:guid}/settings")]
    [ProducesResponseType(typeof(SiteSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateSettings(
        Guid id,
        [FromBody] UpdateSiteSettingsRequest request,
        CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(
            new UpdateSiteSettingsCommand(
                id,
                request.PreviewUrlTemplate,
                request.VersioningEnabled,
                request.WorkflowEnabled,
                request.SchedulingEnabled,
                request.PreviewEnabled,
                request.AiEnabled,
                request.CorsOrigins,
                request.Locales),
            ct));

    // ── Config Entries ────────────────────────────────────────────────────────
    //
    // Generic key-value store for all per-site configuration (AI, media, webhooks, etc.).
    // Filter by ?category=ai to see only AI settings; the Admin UI AI Settings tab does this.
    // Read at runtime via ISettingsReader — no separate endpoint needed for reading by the app.

    /// <summary>
    /// Returns all config entries for a site.
    /// Pass <c>?category=ai</c> to see only AI settings (provider, prompts, budget, safety).
    /// Secret values (API keys) are redacted and returned as "***".
    /// </summary>
    [HttpGet("{id:guid}/config")]
    [ProducesResponseType(typeof(SiteConfigEntriesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfig(
        Guid id, [FromQuery] string? category = null, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new GetSiteConfigEntriesQuery(id, category), ct));

    /// <summary>
    /// Creates or updates a single config entry.
    /// Set <c>isSecret: true</c> for credentials — they are redacted in all read responses.
    /// </summary>
    [HttpPut("{id:guid}/config/{key}")]
    [ProducesResponseType(typeof(ConfigEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpsertConfigEntry(
        Guid id,
        string key,
        [FromBody] UpsertConfigEntryRequest request,
        CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(
            new UpsertSiteConfigEntryCommand(id, key, request.Value, request.Category, request.IsSecret), ct));

    /// <summary>Removes a config entry from a site. No-op if the key is absent.</summary>
    [HttpDelete("{id:guid}/config/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConfigEntry(
        Guid id, string key, CancellationToken ct = default) =>
        NoContentOrProblem(await Sender.Send(new DeleteSiteConfigEntryCommand(id, key), ct));

    /// <summary>
    /// Bulk-imports config entries from a JSON payload (e.g. <c>ai.settings.json</c>).
    /// Non-destructive merge: existing keys absent from the payload are preserved.
    /// Secret entries (API keys) must be set separately via <c>PUT /config/{key}</c> — they are
    /// not exported and therefore not importable from the reference file.
    /// </summary>
    [HttpPost("{id:guid}/config/bulk-import")]
    [ProducesResponseType(typeof(SiteConfigEntriesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ImportConfig(
        Guid id,
        [FromBody] ImportConfigRequest request,
        CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(
            new BulkImportSiteConfigEntriesCommand(
                id,
                request.Entries.Select(e => new ImportConfigEntry(e.Key, e.Value, e.Category, e.IsSecret)).ToList()),
            ct));

    /// <summary>
    /// Exports all non-secret config entries as a JSON payload compatible with the import endpoint.
    /// Use this to back up settings or seed another site.
    /// </summary>
    [HttpGet("{id:guid}/config/bulk-export")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExportConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportConfig(
        Guid id, [FromQuery] string? category = null, CancellationToken ct = default)
    {
        var result = await Sender.Send(new GetSiteConfigEntriesQuery(id, category), ct);
        if (!result.IsSuccess)
            return Problem(result.Error.Message, statusCode: StatusCodes.Status404NotFound);

        // Secrets are excluded — they must be re-entered after import.
        var exportPayload = new ExportConfigResponse(
            SiteId: id,
            Category: category,
            ExportedAt: DateTimeOffset.UtcNow,
            Entries: result.Value.Entries
                .Where(e => !e.IsSecret)
                .Select(e => new ImportConfigEntryItem(e.Key, e.Value, e.Category, false))
                .ToList());

        return new JsonResult(exportPayload) { StatusCode = StatusCodes.Status200OK };
    }
}

// ── Request / Response DTOs ───────────────────────────────────────────────────

public sealed record UpdateSiteRequest(
    string Name,
    string DefaultLocale,
    string? CustomDomain);

public sealed record UpdateSiteSettingsRequest(
    string? PreviewUrlTemplate,
    bool VersioningEnabled,
    bool WorkflowEnabled,
    bool SchedulingEnabled,
    bool PreviewEnabled,
    bool AiEnabled,
    IReadOnlyList<string> CorsOrigins,
    IReadOnlyList<string> Locales);

public sealed record UpsertConfigEntryRequest(
    string Value,
    string Category = "general",
    bool IsSecret = false);

public sealed record ImportConfigRequest(IReadOnlyList<ImportConfigEntryItem> Entries);

public sealed record ImportConfigEntryItem(
    string Key,
    string Value,
    string Category = "general",
    bool IsSecret = false);

public sealed record ExportConfigResponse(
    Guid SiteId,
    string? Category,
    DateTimeOffset ExportedAt,
    IReadOnlyList<ImportConfigEntryItem> Entries);

