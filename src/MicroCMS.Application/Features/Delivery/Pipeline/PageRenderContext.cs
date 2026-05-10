using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Delivery.Pipeline;

/// <summary>
/// Mutable context bag that flows through the page render pipeline.
///
/// Each <see cref="IPageRenderStep"/> reads from and writes to this object.
/// No step depends on the internal logic of another step — they only share data
/// through this context, making steps fully independent and reorderable.
///
/// Lifecycle: created by <see cref="PageRenderPipeline"/> per request, discarded after
/// the pipeline completes. Not thread-safe (single-request scope only).
/// </summary>
public sealed class PageRenderContext
{
    // ── Input (set before pipeline runs) ─────────────────────────────────

    /// <summary>Site the request targets.</summary>
    public required SiteId SiteId { get; init; }

    /// <summary>Slug of the page to render (e.g. "about-us").</summary>
    public required string Slug { get; init; }

    // ── Resolved by steps ─────────────────────────────────────────────────

    /// <summary>Populated by <c>ResolvePageStep</c>.</summary>
    public Page? Page { get; set; }

    /// <summary>Populated by <c>ResolveSiteTemplateStep</c>. Null when no template applies.</summary>
    public SiteTemplate? SiteTemplate { get; set; }

    /// <summary>Populated by <c>ResolvePageTemplateStep</c>. Null when the page has no custom placements.</summary>
    public PageTemplate? PageTemplate { get; set; }

    /// <summary>Populated by <c>ResolveLayoutStep</c>. Null when no layout is configured (headless mode).</summary>
    public Layout? Layout { get; set; }

    /// <summary>Populated by <c>ResolveEntryStep</c>. Null for collection pages or unlinked static pages.</summary>
    public Entry? LinkedEntry { get; set; }

    /// <summary>
    /// Flattened field bag from <see cref="LinkedEntry"/>. Populated by <c>ResolvePageFieldsStep</c>.
    /// Used to resolve <c>{{page:fieldName}}</c> tokens in the layout shell.
    /// </summary>
    public IReadOnlyDictionary<string, string> PageFields { get; set; } =
        new Dictionary<string, string>();

    /// <summary>Per-zone HTML fragments. Populated by <c>RenderZonesStep</c>.</summary>
    public IReadOnlyDictionary<string, string> Zones { get; set; } =
        new Dictionary<string, string>();

    /// <summary>Final full-page HTML. Populated by <c>RenderLayoutStep</c>.</summary>
    public string? Html { get; set; }

    /// <summary>Resolved SEO values. Populated by <c>ResolveSeoStep</c>.</summary>
    public SeoDto? Seo { get; set; }

    // ── Short-circuit support ─────────────────────────────────────────────

    /// <summary>
    /// When a step sets this, the pipeline stops calling subsequent steps and
    /// returns <see cref="Result"/> immediately. Used by the cache-read step to
    /// short-circuit the full render pipeline on a cache hit.
    /// </summary>
    public RenderedPageDto? Result { get; set; }
}
