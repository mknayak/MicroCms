using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Delivery.Caching;

namespace MicroCMS.Application.Features.Delivery.Pipeline.Steps;

/// <summary>
/// Step 1 — Cache read.
///
/// Checks the distributed cache for a previously rendered page.
/// On a hit, sets <see cref="PageRenderContext.Result"/> and short-circuits the pipeline.
/// On a miss, calls <c>next()</c> so the remaining steps execute.
/// </summary>
internal sealed class ResolveCacheStep(ICacheService cache) : IPageRenderStep
{
    public async Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct)
    {
        var key = DeliveryCacheKeys.RenderedPage(ctx.SiteId.Value, ctx.Slug);
        var cached = await cache.GetAsync<MicroCMS.Application.Features.Delivery.Dtos.RenderedPageDto>(key, ct);

        if (cached is not null)
        {
            ctx.Result = cached;
            return; // short-circuit — skip remaining steps
        }

        await next();
    }
}
