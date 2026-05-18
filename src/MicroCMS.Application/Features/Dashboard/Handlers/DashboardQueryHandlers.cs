using MediatR;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Dashboard.Dtos;
using MicroCMS.Application.Features.Dashboard.Queries;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.Specifications.Identity;
using MicroCMS.Domain.Specifications.Media;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Dashboard.Handlers;

internal sealed class GetDashboardStatsQueryHandler(
    IRepository<Entry, EntryId> entryRepo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo,
    IRepository<MediaAsset, MediaAssetId> mediaRepo,
    IRepository<User, UserId> userRepo,
    ICurrentUser currentUser)
    : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    public async Task<Result<DashboardStatsDto>> Handle(
        GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<DashboardStatsDto>(
                new Error("Dashboard.NoSiteContext", "No site context is available for the current user."));

        var totalEntries     = await entryRepo.CountAsync(new EntriesBySiteSpec(siteId, statusFilter: null), cancellationToken);
        var publishedEntries = await entryRepo.CountAsync(new EntriesBySiteSpec(siteId, statusFilter: EntryStatus.Published.ToString()), cancellationToken);
        var draftEntries     = await entryRepo.CountAsync(new EntriesBySiteSpec(siteId, statusFilter: EntryStatus.Draft.ToString()), cancellationToken);
        var totalAssets      = await mediaRepo.CountAsync(new MediaAssetsBySiteSpec(siteId), cancellationToken);
        var contentTypes     = await contentTypeRepo.CountAsync(new ContentTypesBySiteSpec(siteId), cancellationToken);
        var totalUsers       = await userRepo.CountAsync(new AllUsersCountSpec(), cancellationToken);

        return Result.Success(new DashboardStatsDto(
            TotalEntries:     totalEntries,
            PublishedEntries: publishedEntries,
            DraftEntries:     draftEntries,
            TotalAssets:      totalAssets,
            TotalUsers:       totalUsers,
            ContentTypes:     contentTypes));
    }
}

internal sealed class GetDashboardActivityQueryHandler(
    IRepository<Entry, EntryId> entryRepo,
    IRepository<User, UserId> userRepo,
    ICurrentUser currentUser)
    : IRequestHandler<GetDashboardActivityQuery, Result<PagedList<DashboardActivityItemDto>>>
{
    public async Task<Result<PagedList<DashboardActivityItemDto>>> Handle(
        GetDashboardActivityQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<PagedList<DashboardActivityItemDto>>(
                new Error("Dashboard.NoSiteContext", "No site context is available for the current user."));

        var listSpec = new EntriesBySiteSpec(
            siteId,
            statusFilter: null,
            contentTypeId: null,
            locale: null,
            folderId: null,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize);

        var countSpec = new EntriesBySiteSpec(siteId, statusFilter: null);

        var entries    = await entryRepo.ListAsync(listSpec, cancellationToken);
        var totalCount = await entryRepo.CountAsync(countSpec, cancellationToken);

        // Build author-name lookup (one query per distinct author — acceptable for small pages)
        var authorIds   = entries.Select(e => e.AuthorId).Distinct().ToList();
        var authorNames = new Dictionary<Guid, string>();
        foreach (var authorId in authorIds)
        {
            var user = await userRepo.GetByIdAsync(new UserId(authorId), cancellationToken);
            if (user is not null)
                authorNames[authorId] = user.DisplayName.Value;
        }

        var items = entries.Select(e => new DashboardActivityItemDto(
            Id:            e.Id.Value,
            Type:          MapActivityType(e.Status),
            Description:   $"Entry '{e.Slug.Value}' was {MapActivityType(e.Status).ToLowerInvariant()}",
            ActorName:     authorNames.GetValueOrDefault(e.AuthorId, "Unknown"),
            ActorAvatarUrl: null,
            EntityId:      e.Id.Value,
            EntityType:    "Entry",
            EntityTitle:   e.Slug.Value,
            CreatedAt:     e.UpdatedAt)).ToList();

        return Result.Success(PagedList<DashboardActivityItemDto>.Create(
            items, request.PageNumber, request.PageSize, totalCount));
    }

    private static string MapActivityType(EntryStatus status) => status switch
    {
        EntryStatus.Published  => "Published",
        EntryStatus.Draft      => "Updated",
        EntryStatus.Archived   => "Archived",
        _                      => "Updated",
    };
}
