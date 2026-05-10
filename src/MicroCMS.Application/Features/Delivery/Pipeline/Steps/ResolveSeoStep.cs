using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Domain.Aggregates.Pages;

namespace MicroCMS.Application.Features.Delivery.Pipeline.Steps;

/// <summary>
/// Step 6 — SEO resolution.
///
/// Derives <see cref="SeoDto"/> from the page's <c>Seo</c> value object.
/// Falls back to <c>Page.Title</c> as the meta title when no explicit SEO is set.
/// </summary>
internal sealed class ResolveSeoStep : IPageRenderStep
{
    public async Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct)
    {
        var pageSeo = ctx.Page!.Seo;
        ctx.Seo = new SeoDto(
            pageSeo.MetaTitle ?? ctx.Page.Title,
            pageSeo.MetaDescription,
            pageSeo.OgImage,
            pageSeo.CanonicalUrl);

        await next();
    }
}
