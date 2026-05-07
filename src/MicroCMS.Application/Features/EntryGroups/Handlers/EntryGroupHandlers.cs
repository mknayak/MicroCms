using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.EntryGroups.Commands;
using MicroCMS.Application.Features.EntryGroups.Dtos;
using MicroCMS.Application.Features.EntryGroups.Queries;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.EntryGroups.Handlers;

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class EntryGroupMapper
{
    internal static EntryGroupDto ToDto(EntryGroup g) => new(
        g.Id.Value,
        g.SiteId.Value,
        g.ContentTypeId.Value,
        g.Handle,
        g.Title,
        g.Description,
        g.ImageAssetId?.Value,
        g.Members.Select(m => m.EntryId.Value).ToList(),
        g.CreatedAt,
        g.UpdatedAt);

    internal static EntryGroupListItemDto ToListItemDto(EntryGroup g) => new(
        g.Id.Value,
        g.SiteId.Value,
        g.ContentTypeId.Value,
        g.Handle,
        g.Title,
        g.Description,
        g.ImageAssetId?.Value,
        g.Members.Count,
        g.UpdatedAt);
}

// ── List ──────────────────────────────────────────────────────────────────────

internal sealed class ListEntryGroupsQueryHandler(
    IRepository<EntryGroup, EntryGroupId> repo,
    ICurrentUser currentUser)
    : IRequestHandler<ListEntryGroupsQuery, Result<IReadOnlyList<EntryGroupListItemDto>>>
{
    public async Task<Result<IReadOnlyList<EntryGroupListItemDto>>> Handle(
        ListEntryGroupsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<IReadOnlyList<EntryGroupListItemDto>>(
                Error.Validation("Auth.NoSiteContext", "No site context in token."));

        var groups = await repo.ListAsync(
            new EntryGroupsBySiteSpec(siteId, request.ContentTypeId), cancellationToken);

        return Result.Success<IReadOnlyList<EntryGroupListItemDto>>(
            groups.Select(EntryGroupMapper.ToListItemDto).ToList());
    }
}

// ── Get ───────────────────────────────────────────────────────────────────────

internal sealed class GetEntryGroupQueryHandler(
    IRepository<EntryGroup, EntryGroupId> repo)
    : IRequestHandler<GetEntryGroupQuery, Result<EntryGroupDto>>
{
    public async Task<Result<EntryGroupDto>> Handle(
        GetEntryGroupQuery request,
        CancellationToken cancellationToken)
    {
        var group = await repo.GetByIdAsync(new EntryGroupId(request.GroupId), cancellationToken)
            ?? throw new NotFoundException(nameof(EntryGroup), request.GroupId);

        return Result.Success(EntryGroupMapper.ToDto(group));
    }
}

// ── Create ────────────────────────────────────────────────────────────────────

internal sealed class CreateEntryGroupCommandHandler(
    IRepository<EntryGroup, EntryGroupId> repo,
    ICurrentUser currentUser)
    : IRequestHandler<CreateEntryGroupCommand, Result<EntryGroupDto>>
{
    public async Task<Result<EntryGroupDto>> Handle(
        CreateEntryGroupCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<EntryGroupDto>(
                Error.Validation("Auth.NoSiteContext", "No site context in token."));

        var group = EntryGroup.Create(
            currentUser.TenantId,
            siteId,
            new ContentTypeId(request.ContentTypeId),
            request.Handle,
            request.Title,
            request.Description,
            request.ImageAssetId.HasValue ? new MediaAssetId(request.ImageAssetId.Value) : null);

        if (request.MemberEntryIds is { Count: > 0 })
            group.SetMembers(request.MemberEntryIds.Select(id => new EntryId(id)));

        await repo.AddAsync(group, cancellationToken);
        return Result.Success(EntryGroupMapper.ToDto(group));
    }
}

// ── Update ────────────────────────────────────────────────────────────────────

internal sealed class UpdateEntryGroupCommandHandler(
    IRepository<EntryGroup, EntryGroupId> repo)
    : IRequestHandler<UpdateEntryGroupCommand, Result<EntryGroupDto>>
{
    public async Task<Result<EntryGroupDto>> Handle(
        UpdateEntryGroupCommand request,
        CancellationToken cancellationToken)
    {
        var group = await repo.GetByIdAsync(new EntryGroupId(request.GroupId), cancellationToken)
            ?? throw new NotFoundException(nameof(EntryGroup), request.GroupId);

        group.Update(
            request.Title,
            request.Description,
            request.ImageAssetId.HasValue ? new MediaAssetId(request.ImageAssetId.Value) : null);

        if (request.MemberEntryIds is not null)
            group.SetMembers(request.MemberEntryIds.Select(id => new EntryId(id)));

        repo.Update(group);
        return Result.Success(EntryGroupMapper.ToDto(group));
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────

internal sealed class DeleteEntryGroupCommandHandler(
    IRepository<EntryGroup, EntryGroupId> repo)
    : IRequestHandler<DeleteEntryGroupCommand, Result>
{
    public async Task<Result> Handle(
        DeleteEntryGroupCommand request,
        CancellationToken cancellationToken)
    {
        var group = await repo.GetByIdAsync(new EntryGroupId(request.GroupId), cancellationToken)
            ?? throw new NotFoundException(nameof(EntryGroup), request.GroupId);

        repo.Remove(group);
        return Result.Success();
    }
}
