using MicroCMS.Application.Features.Delivery.Rendering;
using MicroCMS.Domain.Aggregates.Pages;

namespace MicroCMS.Application.Features.Delivery.Pipeline.Steps;

/// <summary>
/// Step 9 — Layout rendering.
///
/// Injects the rendered zone HTML into the layout shell, resolves all
/// <c>{{namespace:key}}</c> tokens (page, seo, site, user, template), and
/// produces the final complete HTML document stored in <see cref="PageRenderContext.Html"/>.
///
/// When no layout was resolved (headless mode), this step is a no-op and
/// the caller receives per-zone fragments via <see cref="PageRenderContext.Zones"/>.
/// </summary>
internal sealed class RenderLayoutStep(IComponentRenderingService renderer) : IPageRenderStep
{
    public async Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct)
    {
        if (ctx.Layout is not null)
        {
            var renderContext = new RenderContext
            {
                SiteId        = ctx.SiteId,
                TenantId      = ctx.Page!.TenantId,
                PageSlug      = ctx.Page.Slug.Value,
                PageTitle     = ctx.Page.Title,
                PagePublishedAt = null,
                TemplateKey   = ctx.Layout.Key,
                TemplateName  = ctx.Layout.Name,
                PageFields    = ctx.PageFields,
            };

            ctx.Html = await renderer.RenderLayoutAsync(
                ctx.Layout,
                ctx.Zones,
                renderContext,
                seoTitle:       ctx.Seo?.Title,
                seoDescription: ctx.Seo?.Description,
                seoOgImage:     ctx.Seo?.OgImage,
                cancellationToken: ct);
        }

        await next();
    }
}
