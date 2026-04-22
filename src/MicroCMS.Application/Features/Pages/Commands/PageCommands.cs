using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Pages.Commands;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record PageDto(
    Guid Id,
    Guid SiteId,
    string Title,
    string Slug,
    string PageType,
    Guid? ParentId,
    Guid? LinkedEntryId,
    Guid? CollectionContentTypeId,
    string? RoutePattern,
    int Depth);

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

// ── Queries ───────────────────────────────────────────────────────────────────

[HasPolicy(ContentPolicies.EntryRead)]
public sealed record GetSiteTreeQuery(Guid SiteId) : IQuery<IReadOnlyList<PageTreeNode>>;

public sealed record PageTreeNode(
    Guid Id, string Title, string Slug, string PageType, Guid? ParentId,
    int Depth, IReadOnlyList<PageTreeNode> Children);

// ── Handlers ──────────────────────────────────────────────────────────────────

internal sealed class CreateStaticPageCommandHandler(
    IRepository<Page, PageId> pageRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CreateStaticPageCommand, Result<PageDto>>
{
    public async Task<Result<PageDto>> Handle(CreateStaticPageCommand request, CancellationToken cancellationToken)
    {
        var parentId = request.ParentId.HasValue ? new PageId(request.ParentId.Value) : (PageId?)null;
        var linkedEntryId = request.LinkedEntryId.HasValue ? new EntryId(request.LinkedEntryId.Value) : (EntryId?)null;
        var page = Page.CreateStatic(
             currentUser.TenantId, new SiteId(request.SiteId),
              request.Title, Slug.Create(request.Slug),
     parentId, linkedEntryId, depth: request.ParentId.HasValue ? 1 : 0);
        await pageRepository.AddAsync(page, cancellationToken);
        return Result.Success(PageMapper.ToDto(page));
    }
}

internal sealed class CreateCollectionPageCommandHandler(
    IRepository<Page, PageId> pageRepository,
    ICurrentUser currentUser)
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
        var spec = new PagesBySiteSpec(new SiteId(request.SiteId));
        var pages = await pageRepository.ListAsync(spec, cancellationToken);
        return Result.Success(BuildTree(pages, parentId: null));
    }

    private static IReadOnlyList<PageTreeNode> BuildTree(IReadOnlyList<Page> all, PageId? parentId) =>
        all.Where(p => p.ParentId == parentId)
            .Select(p => new PageTreeNode(
                p.Id.Value, p.Title, p.Slug.Value, p.PageType.ToString(),
                p.ParentId?.Value, p.Depth, BuildTree(all, p.Id)))
            .ToList().AsReadOnly();
}

internal static class PageMapper
{
    internal static PageDto ToDto(Page p) => new(
        p.Id.Value, p.SiteId.Value, p.Title, p.Slug.Value, p.PageType.ToString(),
        p.ParentId?.Value, p.LinkedEntryId?.Value, p.CollectionContentTypeId?.Value,
        p.RoutePattern, p.Depth);
}
