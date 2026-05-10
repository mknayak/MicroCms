using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Delivery.Caching;
using MicroCMS.Application.Features.Delivery.Dtos;

namespace MicroCMS.Application.Features.Delivery.Pipeline.Steps;

/// <summary>
/// Step 10 — Cache write.
///
/// Persists the rendered <see cref="RenderedPageDto"/> in the distributed cache
/// under a per-page key tagged with the site ID, so the authoring worker can
/// invalidate all pages for a site with a single tag-based eviction.
///
/// Only full HTML responses are cached; headless (zone-only) responses are excluded
/// because they may carry dynamic per-request state.
/// </summary>
internal sealed class WriteCacheStep(ICacheService cache) : IPageRenderStep
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(24);

    public async Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct)
    {
        await next(); // let all prior steps complete first

        // Only cache when a full layout-rendered HTML response was produced.
        if (ctx.Html is null || ctx.Page is null) return;

        var dto = new RenderedPageDto(
            ctx.Page.Id.Value,
            ctx.Page.Slug.Value,
            ctx.Page.Title,
            ctx.Html,
            null,   // Zones not stored; HTML is the canonical cached form
            ctx.Seo);

        var key = DeliveryCacheKeys.RenderedPage(ctx.SiteId.Value, ctx.Slug);
        await cache.SetWithTagAsync(
            key,
            dto,
            DeliveryCacheKeys.SiteTag(ctx.SiteId.Value),
            expiry: DefaultExpiry,
            cancellationToken: ct);
    }
}
