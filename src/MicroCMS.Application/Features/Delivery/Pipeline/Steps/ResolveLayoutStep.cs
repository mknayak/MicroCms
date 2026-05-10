using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Layouts;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Delivery.Pipeline.Steps;

/// <summary>
/// Step 5 — Layout resolution.
///
/// Determines the <see cref="Layout"/> shell to wrap the rendered zones in.
///
/// Priority (first match wins):
///   1. <c>SiteTemplate.LayoutId</c> — when a site template was resolved
///   2. <c>Page.LayoutId</c>         — explicit page-level override
///   3. Default layout for the site
///   4. First layout for the site (last resort)
/// </summary>
internal sealed class ResolveLayoutStep(IRepository<Layout, LayoutId> layoutRepo) : IPageRenderStep
{
    public async Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct)
    {
        var page = ctx.Page!;
        var siteId = ctx.SiteId;

        if (ctx.SiteTemplate is not null)
        {
            var layout = await layoutRepo.GetByIdAsync(ctx.SiteTemplate.LayoutId, ct);
            if (layout is not null) { ctx.Layout = layout; await next(); return; }
        }

        if (page.LayoutId.HasValue)
        {
            var layout = await layoutRepo.GetByIdAsync(page.LayoutId.Value, ct);
            if (layout is not null) { ctx.Layout = layout; await next(); return; }
        }

        var defaults = await layoutRepo.ListAsync(new DefaultLayoutBySiteSpec(siteId), ct);
        if (defaults.Count > 0) { ctx.Layout = defaults[0]; await next(); return; }

        var all = await layoutRepo.ListAsync(new LayoutsBySiteSpec(siteId), ct);
        ctx.Layout = all.FirstOrDefault();

        await next();
    }
}
