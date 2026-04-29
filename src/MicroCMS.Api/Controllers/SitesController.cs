using MicroCMS.Application.Features.Sites.Commands;
using MicroCMS.Application.Features.Sites.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Per-site management: general properties and feature-flag settings.
/// Route: /api/v1/sites
/// </summary>
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
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

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
