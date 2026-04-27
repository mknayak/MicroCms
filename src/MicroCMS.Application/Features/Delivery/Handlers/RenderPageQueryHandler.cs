using System.Text;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Application.Features.Delivery.Queries;
using MicroCMS.Application.Features.Delivery.Rendering;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Components;
using MicroCMS.Domain.Specifications.Delivery;
using MicroCMS.Domain.Specifications.Layouts;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Delivery.Handlers;

/// <summary>
/// Renders a full page by walking:
///   Slug → Page → PageTemplate → ComponentPlacements → ComponentRenderer → Layout shell.
///
/// SEO resolution order (first non-null wins per field):
///   1. Page.Seo  — page-level override set via PUT /pages/{id}/seo
///   2. LinkedEntry.Seo  — the entry linked to a Static page (if any)
///   3. Page.Title  — used as seo:title fallback when nothing else is set
/// </summary>
internal sealed class RenderPageBySlugQueryHandler(
    IRepository<Page, PageId> pageRepo,
    IRepository<PageTemplate, PageTemplateId> templateRepo,
    IRepository<Component, ComponentId> compRepo,
    IRepository<ComponentItem, ComponentItemId> itemRepo,
    IRepository<Layout, LayoutId> layoutRepo,
    IRepository<Entry, EntryId> entryRepo,
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

        // ── 2. Load PageTemplate ──────────────────────────────────────────
        var templates = await templateRepo.ListAsync(
          new PageTemplateByPageSpec(page.Id), cancellationToken);
        var template = templates.FirstOrDefault();

  // ── 3+4. Render placements → zone HTML ────────────────────────────
    var zones = await RenderZonesAsync(template, compRepo, itemRepo, renderer, cancellationToken);

    // ── 5. Resolve SEO ────────────────────────────────────────────────
        var seo = await ResolveSeoAsync(page, entryRepo, cancellationToken);

        // ── 6. Resolve Layout (page override → site default → none) ───────
        var layout = await ResolveLayoutAsync(page, siteId, layoutRepo, cancellationToken);

        // ── 7. Compose final output ───────────────────────────────────────
        string? html = null;
  if (layout is not null)
   html = await renderer.RenderLayoutAsync(
layout, zones,
  seoTitle: seo.Title,
       seoDescription: seo.Description,
             seoOgImage: seo.OgImage,
          cancellationToken: cancellationToken);

        return Result.Success(new RenderedPageDto(
            page.Id.Value,
            page.Slug.Value,
  page.Title,
         html,
            html is null ? zones : null,
     seo));
    }

    // ── SEO resolution ────────────────────────────────────────────────────

  /// <summary>
    /// Resolves SEO using a three-level fallback:
    ///   Page.Seo → LinkedEntry.Seo → Page.Title as title fallback.
    /// </summary>
    private static async Task<SeoDto> ResolveSeoAsync(
        Page page,
 IRepository<Entry, EntryId> entryRepo,
        CancellationToken ct)
    {
        var pageSeo = page.Seo;
        var entrySeo = await LoadLinkedEntrySeoAsync(page, entryRepo, ct);
        return MergeSeo(page.Title, pageSeo, entrySeo);
    }

    private static async Task<SeoMetadata?> LoadLinkedEntrySeoAsync(
        Page page,
      IRepository<Entry, EntryId> entryRepo,
        CancellationToken ct)
    {
        if (!page.LinkedEntryId.HasValue) return null;
        var entry = await entryRepo.GetByIdAsync(page.LinkedEntryId.Value, ct);
        return entry?.Seo;
  }

    private static SeoDto MergeSeo(string pageTitle, SeoMetadata pageSeo, SeoMetadata? entrySeo)
    {
        var title = pageSeo.MetaTitle
          ?? entrySeo?.MetaTitle
                 ?? pageTitle;
   var description = pageSeo.MetaDescription ?? entrySeo?.MetaDescription;
        var ogImage     = pageSeo.OgImage          ?? entrySeo?.OgImage;
        var canonical   = pageSeo.CanonicalUrl     ?? entrySeo?.CanonicalUrl;
     return new SeoDto(title, description, ogImage, canonical);
    }

    // ── Zone rendering ────────────────────────────────────────────────────

    private static async Task<IReadOnlyDictionary<string, string>> RenderZonesAsync(
        PageTemplate? template,
        IRepository<Component, ComponentId> compRepo,
IRepository<ComponentItem, ComponentItemId> itemRepo,
 IComponentRenderingService renderer,
      CancellationToken ct)
    {
        if (template is null || template.Placements.Count == 0)
    return new Dictionary<string, string>();

        var zoneHtml = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);

        foreach (var placement in template.Placements.OrderBy(p => p.SortOrder))
        {
         var comp = await compRepo.GetByIdAsync(placement.ComponentId, ct);
            if (comp is null) continue;

     var items = await itemRepo.ListAsync(
    new ComponentItemsByComponentAndStatusSpec(comp.Id, ComponentItemStatus.Published, 1, int.MaxValue),
              ct);

   var sb = zoneHtml.TryGetValue(placement.Zone, out var existing)
        ? existing
     : (zoneHtml[placement.Zone] = new StringBuilder());

  foreach (var item in items)
            {
   var fragment = await renderer.RenderComponentAsync(comp, item, ct);
          sb.Append(fragment);
            }
    }

        return zoneHtml.ToDictionary(
      kv => kv.Key,
            kv => kv.Value.ToString(),
   StringComparer.OrdinalIgnoreCase);
    }

    // ── Layout resolution ─────────────────────────────────────────────────

    private static async Task<Layout?> ResolveLayoutAsync(
        Page page,
  SiteId siteId,
        IRepository<Layout, LayoutId> layoutRepo,
      CancellationToken ct)
    {
        if (page.LayoutId.HasValue)
     {
      var layout = await layoutRepo.GetByIdAsync(page.LayoutId.Value, ct);
            if (layout is not null) return layout;
        }

        var defaults = await layoutRepo.ListAsync(new DefaultLayoutBySiteSpec(siteId), ct);
        return defaults.FirstOrDefault();
    }
}
