using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Delivery.Pipeline.Steps;

/// <summary>
/// Step 3 — SiteTemplate resolution.
///
/// Resolves the effective <see cref="SiteTemplate"/> using the hierarchy:
///   1. <c>Page.SiteTemplateId</c>  — explicit page-level override
///   2. <c>ContentType.SiteTemplateId</c>  — default for the page's content type
///   3. <c>null</c>  — no site template; layout fallback applies
/// </summary>
internal sealed class ResolveSiteTemplateStep(
    IRepository<SiteTemplate, SiteTemplateId> siteTemplateRepo,
    IRepository<Entry, EntryId> entryRepo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo)
    : IPageRenderStep
{
    public async Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct)
    {
        var page = ctx.Page!;

        // Level 1: explicit page override
        if (page.SiteTemplateId.HasValue)
        {
            var t = await siteTemplateRepo.GetByIdAsync(page.SiteTemplateId.Value, ct);
            if (t is not null)
            {
                ctx.SiteTemplate = t;
                await next();
                return;
            }
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
            var contentType = await contentTypeRepo.GetByIdAsync(contentTypeId.Value, ct);
            if (contentType?.SiteTemplateId is { } ctTemplateId)
            {
                var t = await siteTemplateRepo.GetByIdAsync(ctTemplateId, ct);
                if (t is not null)
                {
                    ctx.SiteTemplate = t;
                    await next();
                    return;
                }
            }
        }

        // Level 3: no site template resolved
        ctx.SiteTemplate = null;
        await next();
    }
}
