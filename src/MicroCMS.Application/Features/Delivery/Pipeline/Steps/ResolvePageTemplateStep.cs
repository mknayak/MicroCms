using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Layouts;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Delivery.Pipeline.Steps;

/// <summary>
/// Step 4 — PageTemplate resolution.
///
/// Loads the page-specific <see cref="PageTemplate"/> (its component placements).
/// Sets <see cref="PageRenderContext.PageTemplate"/> to null when the page has no template.
/// </summary>
internal sealed class ResolvePageTemplateStep(
    IRepository<PageTemplate, PageTemplateId> templateRepo)
    : IPageRenderStep
{
    public async Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct)
    {
        var templates = await templateRepo.ListAsync(new PageTemplateByPageSpec(ctx.Page!.Id), ct);
        ctx.PageTemplate = templates.FirstOrDefault();

        await next();
    }
}
