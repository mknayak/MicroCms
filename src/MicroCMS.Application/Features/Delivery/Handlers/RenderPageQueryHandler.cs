using System.Text;
using System.Text.Json;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Application.Features.Delivery.Queries;
using MicroCMS.Application.Features.Delivery.Rendering;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Components;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.Specifications.Delivery;
using MicroCMS.Domain.Specifications.Layouts;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Delivery.Handlers;

/// <summary>
/// Renders a full page by walking:
///   Slug → Page → SiteTemplate hierarchy → PageTemplate → ComponentPlacements → Layout shell.
///
/// SiteTemplate resolution order (first match wins):
///   1. Page.SiteTemplateId  — explicit page-level override
///   2. ContentType.SiteTemplateId  — default for the page's content type
///   3. None  — fall back to default / first Layout with no shared placements
///
/// SEO resolution order (first non-null wins per field):
///   1. Page.Seo  — page-level override set via PUT /pages/{id}/seo
///   2. Page.Title  — used as seo:title fallback when nothing else is set
/// </summary>
internal sealed class RenderPageBySlugQueryHandler(
    IRepository<Page, PageId> pageRepo,
    IRepository<PageTemplate, PageTemplateId> templateRepo,
    IRepository<SiteTemplate, SiteTemplateId> siteTemplateRepo,
    IRepository<Component, ComponentId> compRepo,
    IRepository<Entry, EntryId> entryRepo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo,
    IRepository<Layout, LayoutId> layoutRepo,
    IComponentRenderingService renderer)
    : IRequestHandler<RenderPageBySlugQuery, Result<RenderedPageDto>>
{
    public async Task<Result<RenderedPageDto>> Handle(
        RenderPageBySlugQuery request, CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);

        // ── 1. Resolve Page ───────────────────────────────────────────────
        var pages = await pageRepo.ListAsync(new PageBySlugSpec(siteId, request.Slug), cancellationToken);
        var page = pages.FirstOrDefault()
            ?? throw new NotFoundException(nameof(Page), request.Slug);

        // ── 2. Resolve SiteTemplate via hierarchy ─────────────────────────
        var siteTemplate = await ResolveSiteTemplateAsync(page, entryRepo, contentTypeRepo, siteTemplateRepo, cancellationToken);

        // ── 3. Load page-specific PageTemplate ───────────────────────────
        var templates = await templateRepo.ListAsync(
            new PageTemplateByPageSpec(page.Id), cancellationToken);
        var pageTemplate = templates.FirstOrDefault();

        // ── 4. Render placements → zone HTML ──────────────────────────────
        // SiteTemplate placements are rendered first (base layer), then
        // PageTemplate placements are appended per zone (page-specific overrides).
        var zones = await RenderZonesAsync(siteTemplate, pageTemplate, compRepo, entryRepo, renderer, cancellationToken);

        // ── 5. Resolve SEO ────────────────────────────────────────────────
        var seo = ResolveSeo(page);

        // ── 6. Resolve Layout: SiteTemplate.LayoutId → page override → default ──
        var layout = await ResolveLayoutAsync(page, siteTemplate, siteId, layoutRepo, cancellationToken);

        // ── 7. Build RenderContext for token resolution ───────────────────
        var renderContext = new RenderContext
        {
            SiteId    = siteId,
            TenantId  = page.TenantId,
            PageSlug  = page.Slug.Value,
            PageTitle = page.Title,
            PagePublishedAt = null,
            TemplateKey  = layout?.Key,
            TemplateName = layout?.Name,
        };

        // ── 8. Compose final output ───────────────────────────────────────
        string? html = null;
        if (layout is not null)
            html = await renderer.RenderLayoutAsync(
                layout, zones,
                renderContext,
                seoTitle:       seo.Title,
                seoDescription: seo.Description,
                seoOgImage:     seo.OgImage,
                cancellationToken: cancellationToken);

        return Result.Success(new RenderedPageDto(
            page.Id.Value,
            page.Slug.Value,
            page.Title,
            html,
            html is null ? zones : null,
            seo));
    }

    // ── SiteTemplate hierarchy resolution ────────────────────────────────────

    /// <summary>
    /// Resolves the effective SiteTemplate:
    ///   1. Page.SiteTemplateId (explicit override)
    ///   2. ContentType.SiteTemplateId (type-level default)
    ///   3. null (renderer uses default / first Layout)
    /// </summary>
    private static async Task<SiteTemplate?> ResolveSiteTemplateAsync(
        Page page,
        IRepository<Entry, EntryId> entryRepo,
        IRepository<ContentType, ContentTypeId> contentTypeRepo,
        IRepository<SiteTemplate, SiteTemplateId> siteTemplateRepo,
        CancellationToken ct)
    {
        // Level 1: explicit page override
        if (page.SiteTemplateId.HasValue)
        {
            var t = await siteTemplateRepo.GetByIdAsync(page.SiteTemplateId.Value, ct);
            if (t is not null) return t;
        }

        // Level 2: content type default
        ContentTypeId? contentTypeId = page.CollectionContentTypeId;

        if (contentTypeId is null && page.LinkedEntryId is not null)
        {
            var entry = await entryRepo.GetByIdAsync(page.LinkedEntryId.Value, ct);
            contentTypeId = entry?.ContentTypeId;
        }

        if (contentTypeId is not null)
        {
            var ct2 = await contentTypeRepo.GetByIdAsync(contentTypeId.Value, ct);
            if (ct2?.SiteTemplateId is { } ctTemplateId)
            {
                var t = await siteTemplateRepo.GetByIdAsync(ctTemplateId, ct);
                if (t is not null) return t;
            }
        }

        return null;
    }

    // ── Layout resolution ─────────────────────────────────────────────────

    /// <summary>
    /// Layout priority:
    ///   1. SiteTemplate.LayoutId (when a SiteTemplate was resolved)
    ///   2. Page.LayoutId (explicit page override)
    ///   3. Default layout for the site
    ///   4. First layout for the site
    /// </summary>
    private static async Task<Layout?> ResolveLayoutAsync(
        Page page,
        SiteTemplate? siteTemplate,
        SiteId siteId,
        IRepository<Layout, LayoutId> layoutRepo,
        CancellationToken ct)
    {
        if (siteTemplate is not null)
        {
            var layout = await layoutRepo.GetByIdAsync(siteTemplate.LayoutId, ct);
            if (layout is not null) return layout;
        }

        if (page.LayoutId.HasValue)
        {
            var layout = await layoutRepo.GetByIdAsync(page.LayoutId.Value, ct);
            if (layout is not null) return layout;
        }

        var defaults = await layoutRepo.ListAsync(new DefaultLayoutBySiteSpec(siteId), ct);
        if (defaults.Count > 0) return defaults[0];

        // Last resort: first layout for the site
        var all = await layoutRepo.ListAsync(new LayoutsBySiteSpec(siteId), ct);
        return all.FirstOrDefault();
    }

    // ── SEO resolution ────────────────────────────────────────────────────

    private static SeoDto ResolveSeo(Page page)
    {
        var pageSeo = page.Seo;
        return new SeoDto(
            pageSeo.MetaTitle ?? page.Title,
            pageSeo.MetaDescription,
            pageSeo.OgImage,
            pageSeo.CanonicalUrl);
    }

    // ── Zone rendering ────────────────────────────────────────────────────

    /// <summary>
    /// Renders zones in two passes:
    ///   Pass 1 — SiteTemplate placements (shared base layer, rendered first per zone)
    ///   Pass 2 — PageTemplate placements (page-specific, appended after site layer)
    /// Both templates contribute to the same zone dictionary; zones from both are merged.
    /// </summary>
    private static async Task<IReadOnlyDictionary<string, string>> RenderZonesAsync(
        SiteTemplate? siteTemplate,
        PageTemplate? pageTemplate,
        IRepository<Component, ComponentId> compRepo,
        IRepository<Entry, EntryId> entryRepo,
        IComponentRenderingService renderer,
        CancellationToken ct)
    {
        var zoneHtml = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);

        if (siteTemplate is not null && siteTemplate.PlacementsJson != "[]")
            await AppendPlacementsJsonAsync(siteTemplate.PlacementsJson, compRepo, entryRepo, renderer, zoneHtml, ct);

        if (pageTemplate is not null && pageTemplate.Placements.Count > 0)
            await AppendLegacyPlacementsAsync(pageTemplate, compRepo, entryRepo, renderer, zoneHtml, ct);

        return zoneHtml.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Renders placements stored as the flat legacy <see cref="ComponentPlacement"/> list (PageTemplate).</summary>
    private static async Task AppendLegacyPlacementsAsync(
        PageTemplate pageTemplate,
        IRepository<Component, ComponentId> compRepo,
        IRepository<Entry, EntryId> entryRepo,
        IComponentRenderingService renderer,
        Dictionary<string, StringBuilder> zoneHtml,
        CancellationToken ct)
    {
        foreach (var placement in pageTemplate.Placements.OrderBy(p => p.SortOrder))
        {
            var comp = await compRepo.GetByIdAsync(placement.ComponentId, ct);
            if (comp is null || comp.BackingContentTypeId is null) continue;

            var contentTypeId = comp.BackingContentTypeId.Value.Value;
            var items = await entryRepo.ListAsync(
                new EntriesBySiteSpec(comp.SiteId, EntryStatus.Published.ToString(), contentTypeId), ct);

            var sb = zoneHtml.TryGetValue(placement.Zone, out var existing)
                ? existing
                : (zoneHtml[placement.Zone] = new StringBuilder());

            foreach (var item in items)
                sb.Append(await renderer.RenderComponentAsync(comp, item, ct));
        }
    }

    /// <summary>
    /// Renders placements stored as a JSON tree (SiteTemplate.PlacementsJson).
    /// Extracts component-leaf nodes and renders them into their target zones.
    /// </summary>
    private static async Task AppendPlacementsJsonAsync(
        string placementsJson,
        IRepository<Component, ComponentId> compRepo,
        IRepository<Entry, EntryId> entryRepo,
        IComponentRenderingService renderer,
        Dictionary<string, StringBuilder> zoneHtml,
        CancellationToken ct)
    {
        List<PlacementNode>? nodes;
        try
        {
            nodes = System.Text.Json.JsonSerializer.Deserialize<List<PlacementNode>>(
                placementsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException) { return; }

        if (nodes is null) return;

        // Walk tree depth-first; grid-row columns are flattened into their zoneName
        await WalkNodesAsync(nodes, compRepo, entryRepo, renderer, zoneHtml, ct);
    }

    private static async Task WalkNodesAsync(
        IEnumerable<PlacementNode> nodes,
        IRepository<Component, ComponentId> compRepo,
        IRepository<Entry, EntryId> entryRepo,
        IComponentRenderingService renderer,
        Dictionary<string, StringBuilder> zoneHtml,
        CancellationToken ct)
    {
        foreach (var node in nodes.OrderBy(n => n.SortOrder))
        {
            if (node.Type == "component" && node.ComponentId.HasValue)
                await AppendComponentNodeAsync(node, compRepo, entryRepo, renderer, zoneHtml, ct);
            else if (node.Type == "grid-row" && node.Columns is not null)
                await AppendGridRowAsync(node.Columns, compRepo, entryRepo, renderer, zoneHtml, ct);
        }
    }

    private static async Task AppendComponentNodeAsync(
        PlacementNode node,
        IRepository<Component, ComponentId> compRepo,
        IRepository<Entry, EntryId> entryRepo,
        IComponentRenderingService renderer,
        Dictionary<string, StringBuilder> zoneHtml,
        CancellationToken ct)
    {
        var compId = new ComponentId(node.ComponentId!.Value);
        var comp = await compRepo.GetByIdAsync(compId, ct);
        if (comp is null || comp.BackingContentTypeId is null) return;

        var contentTypeId = comp.BackingContentTypeId.Value.Value;
        var items = await entryRepo.ListAsync(
            new EntriesBySiteSpec(comp.SiteId, EntryStatus.Published.ToString(), contentTypeId), ct);

        var sb = zoneHtml.TryGetValue(node.Zone, out var existing)
            ? existing
            : (zoneHtml[node.Zone] = new StringBuilder());

        foreach (var item in items)
            sb.Append(await renderer.RenderComponentAsync(comp, item, ct));
    }

    private static async Task AppendGridRowAsync(
        IEnumerable<GridColumn> columns,
        IRepository<Component, ComponentId> compRepo,
        IRepository<Entry, EntryId> entryRepo,
        IComponentRenderingService renderer,
        Dictionary<string, StringBuilder> zoneHtml,
        CancellationToken ct)
    {
        foreach (var col in columns)
            await WalkNodesAsync(col.Placements, compRepo, entryRepo, renderer, zoneHtml, ct);
    }

    // ── Page field resolution ─────────────────────────────────────────────

    private static IReadOnlyDictionary<string, string> FlattenFieldsJson(string fieldsJson)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(fieldsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return result;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Null   => string.Empty,
                    JsonValueKind.True   => "true",
                    JsonValueKind.False  => "false",
                    _                    => prop.Value.ToString(),
                };
            }
        }
        catch (JsonException) { }
        return result;
    }

    // ── Private placement node model (mirrors client-side SavePlacementNode) ──

    private sealed class PlacementNode
    {
        public string Type { get; init; } = string.Empty;
        public string Zone { get; init; } = string.Empty;
        public int SortOrder { get; init; }
        public Guid? ComponentId { get; init; }
        public List<GridColumn>? Columns { get; init; }
    }

    private sealed class GridColumn
    {
        public int Span { get; init; }
        public string ZoneName { get; init; } = string.Empty;
        public List<PlacementNode> Placements { get; init; } = [];
    }
}
