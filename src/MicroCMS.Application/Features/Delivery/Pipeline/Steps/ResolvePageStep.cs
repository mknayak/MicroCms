using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Delivery;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Delivery.Pipeline.Steps;

/// <summary>
/// Step 2 — Page resolution.
///
/// Loads the <see cref="Page"/> aggregate matching the site + slug.
/// Throws <see cref="NotFoundException"/> when no published page is found.
/// </summary>
internal sealed class ResolvePageStep(IRepository<Page, PageId> pageRepo) : IPageRenderStep
{
    public async Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct)
    {
        var pages = await pageRepo.ListAsync(new PageBySlugSpec(ctx.SiteId, ctx.Slug), ct);
        ctx.Page = pages.FirstOrDefault() ?? throw new NotFoundException(nameof(Page), ctx.Slug);

        await next();
    }
}
