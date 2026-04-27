using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.Specifications.Layouts;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Pages.Commands;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record PageDto(
    Guid Id, Guid SiteId, string Title, string Slug, string PageType,
    Guid? ParentId, Guid? LinkedEntryId, Guid? CollectionContentTypeId,
    string? RoutePattern, int Depth, Guid? LayoutId,
    /// <summary>Page-level SEO metadata; null when none have been set.</summary>
    PageSeoDto? Seo = null);

public sealed record PageSeoDto(
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    string? OgImage);

public sealed record PageTemplatePlacementDto(Guid Id, Guid ComponentId, string Zone, int SortOrder);

public sealed record PageTemplateDto(
    Guid Id, Guid PageId,
    IReadOnlyList<PageTemplatePlacementDto> Placements,
    DateTimeOffset UpdatedAt);

public sealed record PageTemplatePlacementInput(Guid ComponentId, string Zone, int SortOrder);

// ── Commands ──────────────────────────────────────────────────────────────────

[HasPolicy(ContentPolicies.TenantManage)]
public sealed record CreateStaticPageCommand(
    Guid SiteId, string Title, string Slug,
    Guid? ParentId = null, Guid? LinkedEntryId = null) : ICommand<PageDto>;

[HasPolicy(ContentPolicies.TenantManage)]
public sealed record CreateCollectionPageCommand(
    Guid SiteId, string Title, string Slug,
    Guid ContentTypeId, string RoutePattern,
    Guid? ParentId = null) : ICommand<PageDto>;

[HasPolicy(ContentPolicies.TenantManage)]
public sealed record MovePageCommand(Guid PageId, Guid? NewParentId) : ICommand<PageDto>;

[HasPolicy(ContentPolicies.TenantManage)]
public sealed record DeletePageCommand(Guid PageId) : ICommand;

/// <summary>Assigns or clears the Layout for a page. Pass null LayoutId to use the site default.</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record SetPageLayoutCommand(Guid PageId, Guid? LayoutId) : ICommand<PageDto>;

/// <summary>Creates or fully replaces the PageTemplate (zone placements) for a page.</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record SavePageTemplateCommand(
    Guid PageId,
    IReadOnlyList<PageTemplatePlacementInput> Placements) : ICommand<PageTemplateDto>;

/// <summary>Updates the SEO metadata for a page. Pass null values to clear individual fields.</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record UpdatePageSeoCommand(
    Guid PageId,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    string? OgImage) : ICommand<PageDto>;

/// <summary>Links or clears the entry associated with a Static page.
/// Pass <c>null</c> for <c>EntryId</c> to clear the link.
/// </summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record LinkPageEntryCommand(Guid PageId, Guid? EntryId) : ICommand<PageDto>;

// ── Queries ───────────────────────────────────────────────────────────────────

[HasPolicy(ContentPolicies.EntryRead)]
public sealed record GetSiteTreeQuery(Guid SiteId) : IQuery<IReadOnlyList<PageTreeNode>>;

public sealed record PageTreeNode(
  Guid Id, string Title, string Slug, string PageType,
    Guid? ParentId, int Depth, Guid? LayoutId,
    IReadOnlyList<PageTreeNode> Children);

/// <summary>Returns the full <see cref="PageDto"/> for a single page by its ID.</summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record GetPageQuery(Guid PageId) : IQuery<PageDto>;

[HasPolicy(ContentPolicies.TenantManage)]
public sealed record GetPageTemplateQuery(Guid PageId) : IQuery<PageTemplateDto>;

// ── Handlers ──────────────────────────────────────────────────────────────────

internal sealed class CreateStaticPageCommandHandler(
    IRepository<Page, PageId> pageRepository,
    IRepository<Entry, EntryId> entryRepository,
    IRepository<ContentType, ContentTypeId> contentTypeRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CreateStaticPageCommand, Result<PageDto>>
{
    /// <summary>
    /// Creates a Static page and automatically provisions a linked <c>page</c>-type
    /// entry so authors can immediately populate page-level data (title, description,
    /// hero image, custom CSS/JS, canonical URL).
    ///
    /// The entry slug is derived from the page slug with a <c>page__</c> prefix to
    /// avoid collisions with content entries.  If the built-in <c>page</c> ContentType
    /// does not exist for the site (e.g. an older tenant), the page is still created
    /// without a linked entry rather than failing the whole operation.
    /// </summary>
    public async Task<Result<PageDto>> Handle(CreateStaticPageCommand request, CancellationToken cancellationToken)
    {
        var parentId = request.ParentId.HasValue ? new PageId(request.ParentId.Value) : (PageId?)null;
        var linkedEntryId = request.LinkedEntryId.HasValue ? new EntryId(request.LinkedEntryId.Value) : (EntryId?)null;

        var page = Page.CreateStatic(
            currentUser.TenantId, new SiteId(request.SiteId),
   request.Title, Slug.Create(request.Slug),
            parentId, linkedEntryId, depth: request.ParentId.HasValue ? 1 : 0);

        await pageRepository.AddAsync(page, cancellationToken);

        // Auto-provision a linked entry only when no explicit entry was supplied.
        if (linkedEntryId is null)
        {
            await TryCreateAndLinkPageEntryAsync(page, request, cancellationToken);
        }

        return Result.Success(PageMapper.ToDto(page));
    }

    private async Task TryCreateAndLinkPageEntryAsync(
        Page page, CreateStaticPageCommand request, CancellationToken cancellationToken)
    {
        // Locate the built-in "page" ContentType for this site.
        var siteId = new SiteId(request.SiteId);
        var spec = new ContentTypeByHandleAndSiteSpec(siteId, "page");
        var types = await contentTypeRepository.ListAsync(spec, cancellationToken);
        var pageContentType = types.FirstOrDefault();

        if (pageContentType is null)
        {
            // Graceful degradation: the content type does not exist yet.
            return;
        }

        // Build the entry slug: "page-" + page slug (truncated to 200 chars).
        var rawSlug = $"page-{request.Slug}";
        if (rawSlug.Length > 200) rawSlug = rawSlug[..200];

        // Seed initial fields from the page title so the entry is immediately useful.
        var fieldsJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            title = request.Title,
            description = (string?)null,
            heroImage = (string?)null,
            customCss = (string?)null,
            customJs = (string?)null,
            canonicalUrl = (string?)null,
        });

        var entry = Entry.Create(
         tenantId: currentUser.TenantId,
        siteId: siteId,
    contentTypeId: pageContentType.Id,
    slug: Slug.Create(rawSlug),
      locale: Locale.Create("en"),   // default locale; can be updated later
 authorId: currentUser.UserId,
  fieldsJson: fieldsJson);

        await entryRepository.AddAsync(entry, cancellationToken);

        // Link the freshly created entry to the page.
        page.LinkEntry(entry.Id);
        // No pageRepository.Update call needed — page is already tracked as Added
        // and EF Core will include LinkedEntryId in the INSERT automatically.
    }
}

internal sealed class CreateCollectionPageCommandHandler(
    IRepository<Page, PageId> pageRepository, ICurrentUser currentUser)
    : IRequestHandler<CreateCollectionPageCommand, Result<PageDto>>
{
    public async Task<Result<PageDto>> Handle(CreateCollectionPageCommand request, CancellationToken cancellationToken)
    {
        var parentId = request.ParentId.HasValue ? new PageId(request.ParentId.Value) : (PageId?)null;
        var page = Page.CreateCollection(
        currentUser.TenantId, new SiteId(request.SiteId),
         request.Title, Slug.Create(request.Slug),
         new ContentTypeId(request.ContentTypeId), request.RoutePattern,
            parentId, depth: request.ParentId.HasValue ? 1 : 0);
        await pageRepository.AddAsync(page, cancellationToken);
        return Result.Success(PageMapper.ToDto(page));
    }
}

internal sealed class MovePageCommandHandler(IRepository<Page, PageId> pageRepository)
    : IRequestHandler<MovePageCommand, Result<PageDto>>
{
    public async Task<Result<PageDto>> Handle(MovePageCommand request, CancellationToken cancellationToken)
    {
        var page = await pageRepository.GetByIdAsync(new PageId(request.PageId), cancellationToken)
    ?? throw new NotFoundException(nameof(Page), request.PageId);
        var newParentId = request.NewParentId.HasValue ? new PageId(request.NewParentId.Value) : (PageId?)null;
        page.MoveTo(newParentId, request.NewParentId.HasValue ? 1 : 0);
        pageRepository.Update(page);
        return Result.Success(PageMapper.ToDto(page));
    }
}

internal sealed class DeletePageCommandHandler(IRepository<Page, PageId> pageRepository)
    : IRequestHandler<DeletePageCommand, Result>
{
    public async Task<Result> Handle(DeletePageCommand request, CancellationToken cancellationToken)
    {
        var page = await pageRepository.GetByIdAsync(new PageId(request.PageId), cancellationToken)
            ?? throw new NotFoundException(nameof(Page), request.PageId);
        pageRepository.Remove(page);
        return Result.Success();
    }
}

internal sealed class GetSiteTreeQueryHandler(IRepository<Page, PageId> pageRepository)
    : IRequestHandler<GetSiteTreeQuery, Result<IReadOnlyList<PageTreeNode>>>
{
    public async Task<Result<IReadOnlyList<PageTreeNode>>> Handle(
          GetSiteTreeQuery request, CancellationToken cancellationToken)
    {
        var pages = await pageRepository.ListAsync(
  new PagesBySiteSpec(new SiteId(request.SiteId)), cancellationToken);
        return Result.Success(BuildTree(pages, parentId: null));
    }

    private static IReadOnlyList<PageTreeNode> BuildTree(IReadOnlyList<Page> all, PageId? parentId) =>
        all.Where(p => p.ParentId == parentId)
           .Select(p => new PageTreeNode(
      p.Id.Value, p.Title, p.Slug.Value, p.PageType.ToString(),
    p.ParentId?.Value, p.Depth, p.LayoutId?.Value,
  BuildTree(all, p.Id)))
         .ToList().AsReadOnly();
}

internal sealed class SetPageLayoutCommandHandler(IRepository<Page, PageId> pageRepository)
    : IRequestHandler<SetPageLayoutCommand, Result<PageDto>>
{
    public async Task<Result<PageDto>> Handle(SetPageLayoutCommand request, CancellationToken cancellationToken)
    {
        var page = await pageRepository.GetByIdAsync(new PageId(request.PageId), cancellationToken)
          ?? throw new NotFoundException(nameof(Page), request.PageId);
        page.SetLayout(request.LayoutId.HasValue ? new LayoutId(request.LayoutId.Value) : (LayoutId?)null);
        pageRepository.Update(page);
        return Result.Success(PageMapper.ToDto(page));
    }
}

internal sealed class GetPageTemplateQueryHandler(
    IRepository<PageTemplate, PageTemplateId> templateRepo)
    : IRequestHandler<GetPageTemplateQuery, Result<PageTemplateDto>>
{
    public async Task<Result<PageTemplateDto>> Handle(
   GetPageTemplateQuery request, CancellationToken cancellationToken)
    {
        var templates = await templateRepo.ListAsync(
       new PageTemplateByPageSpec(new PageId(request.PageId)), cancellationToken);
    var template = templates.FirstOrDefault()
         ?? throw new NotFoundException(nameof(PageTemplate), request.PageId);
        return Result.Success(ToDto(template));
    }

    internal static PageTemplateDto ToDto(PageTemplate t) => new(
        t.Id.Value, t.PageId.Value,
        t.Placements.Select(p => new PageTemplatePlacementDto(p.Id, p.ComponentId.Value, p.Zone, p.SortOrder))
       .ToList().AsReadOnly(),
   t.UpdatedAt);
}

internal sealed class SavePageTemplateCommandHandler(
    IRepository<Page, PageId> pageRepo,
    IRepository<PageTemplate, PageTemplateId> templateRepo,
    IRepository<Component, ComponentId> compRepo,
    ICurrentUser currentUser)
    : IRequestHandler<SavePageTemplateCommand, Result<PageTemplateDto>>
{
    public async Task<Result<PageTemplateDto>> Handle(
    SavePageTemplateCommand request, CancellationToken cancellationToken)
    {
        var pageId = new PageId(request.PageId);
      _ = await pageRepo.GetByIdAsync(pageId, cancellationToken)
       ?? throw new NotFoundException(nameof(Page), request.PageId);

 var existing = await templateRepo.ListAsync(new PageTemplateByPageSpec(pageId), cancellationToken);
        PageTemplate template;

        if (existing.Count > 0)
        {
            template = existing[0];
      foreach (var p in template.Placements.ToList())
                template.RemovePlacement(p.Id);
   templateRepo.Update(template);
        }
        else
        {
            template = PageTemplate.Create(currentUser.TenantId, pageId);
      await templateRepo.AddAsync(template, cancellationToken);
        }

    foreach (var input in request.Placements.OrderBy(p => p.SortOrder))
        {
  _ = await compRepo.GetByIdAsync(new ComponentId(input.ComponentId), cancellationToken)
     ?? throw new NotFoundException(nameof(Component), input.ComponentId);
template.AddPlacement(new ComponentId(input.ComponentId), input.Zone, input.SortOrder);
   }

        templateRepo.Update(template);
   return Result.Success(GetPageTemplateQueryHandler.ToDto(template));
    }
}

internal sealed class UpdatePageSeoCommandHandler(IRepository<Page, PageId> pageRepository)
    : IRequestHandler<UpdatePageSeoCommand, Result<PageDto>>
{
    public async Task<Result<PageDto>> Handle(UpdatePageSeoCommand request, CancellationToken cancellationToken)
    {
        var page = await pageRepository.GetByIdAsync(new PageId(request.PageId), cancellationToken)
 ?? throw new NotFoundException(nameof(Page), request.PageId);

        page.UpdateSeo(SeoMetadata.Create(
     request.MetaTitle,
        request.MetaDescription,
       request.CanonicalUrl,
   request.OgImage));

   pageRepository.Update(page);
        return Result.Success(PageMapper.ToDto(page));
    }
}

internal sealed class GetPageQueryHandler(IRepository<Page, PageId> pageRepository)
    : IRequestHandler<GetPageQuery, Result<PageDto>>
{
    public async Task<Result<PageDto>> Handle(GetPageQuery request, CancellationToken cancellationToken)
 {
        var page = await pageRepository.GetByIdAsync(new PageId(request.PageId), cancellationToken)
            ?? throw new NotFoundException(nameof(Page), request.PageId);
        return Result.Success(PageMapper.ToDto(page));
    }
}

internal sealed class LinkPageEntryCommandHandler(IRepository<Page, PageId> pageRepository)
    : IRequestHandler<LinkPageEntryCommand, Result<PageDto>>
{
    public async Task<Result<PageDto>> Handle(LinkPageEntryCommand request, CancellationToken cancellationToken)
    {
   var page = await pageRepository.GetByIdAsync(new PageId(request.PageId), cancellationToken)
         ?? throw new NotFoundException(nameof(Page), request.PageId);

        if (request.EntryId.HasValue)
       page.LinkEntry(new EntryId(request.EntryId.Value));
        else
       page.ClearLinkedEntry();

        pageRepository.Update(page);
        return Result.Success(PageMapper.ToDto(page));
    }
}

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class PageMapper
{
    internal static PageDto ToDto(Page p) => new(
      p.Id.Value, p.SiteId.Value, p.Title, p.Slug.Value, p.PageType.ToString(),
        p.ParentId?.Value, p.LinkedEntryId?.Value, p.CollectionContentTypeId?.Value,
        p.RoutePattern, p.Depth, p.LayoutId?.Value,
      p.Seo is { MetaTitle: null, MetaDescription: null, CanonicalUrl: null, OgImage: null }
    ? null
            : new PageSeoDto(p.Seo.MetaTitle, p.Seo.MetaDescription, p.Seo.CanonicalUrl, p.Seo.OgImage));
}
