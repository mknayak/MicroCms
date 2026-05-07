using MediatR;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Application.Features.Search.EventHandlers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Queries.ListEntries;

/// <summary>Handles <see cref="ListEntriesQuery"/> with a cache-aside read pattern.</summary>
public sealed class ListEntriesQueryHandler(
    IRepository<Entry, EntryId> entryRepository,
    IRepository<ContentType, ContentTypeId> contentTypeRepository,
    ICacheService cacheService,
    ICurrentUser currentUser)
    : IRequestHandler<ListEntriesQuery, Result<PagedList<EntryListItemDto>>>
{
    public async Task<Result<PagedList<EntryListItemDto>>> Handle(
        ListEntriesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<PagedList<EntryListItemDto>>(NoSiteContext());

        var tenantId = currentUser.TenantId;
        var cacheKey = CacheKeys.EntryList(
            tenantId, siteId.Value, request.StatusFilter,
            request.ContentTypeId, request.Locale, request.FolderId,
            request.Search, request.SortBy, request.SortDesc,
            request.PageNumber, request.PageSize);

        var cached = await cacheService.GetAsync<PagedList<EntryListItemDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var listSpec = new EntriesBySiteSpec(
            siteId, request.StatusFilter, request.ContentTypeId,
            request.Locale, request.FolderId, request.PageNumber, request.PageSize,
            request.Search, request.SortBy, request.SortDesc);

        var countSpec = new EntriesBySiteSpec(
            siteId, request.StatusFilter, request.ContentTypeId,
            request.Locale, request.FolderId, request.Search);

        var entries = await entryRepository.ListAsync(listSpec, cancellationToken);
        var totalCount = await entryRepository.CountAsync(countSpec, cancellationToken);

        // Build a lookup of ContentTypeId → DisplayName to populate ContentTypeName on each DTO
        var contentTypeIds = entries.Select(e => e.ContentTypeId).Distinct().ToList();
        var contentTypeNames = new Dictionary<ContentTypeId, string>();
        foreach (var ctId in contentTypeIds)
        {
            var ct = await contentTypeRepository.GetByIdAsync(ctId, cancellationToken);
            if (ct is not null)
                contentTypeNames[ctId] = ct.DisplayName;
        }

        var dtos = entries
            .Select(e => EntryMapper.ToListItemDtoWithContext(
                e,
                contentTypeNames.TryGetValue(e.ContentTypeId, out var name) ? name : null,
                authorName: null))
            .ToList()
            .AsReadOnly();

        var paged = PagedList<EntryListItemDto>.Create(dtos, request.PageNumber, request.PageSize, totalCount);

        await cacheService.SetWithTagAsync(
            cacheKey, paged, CacheTags.TenantEntries(tenantId), cancellationToken: cancellationToken);

        return Result.Success(paged);
    }

    private static Error NoSiteContext() =>
        Error.Validation("Auth.NoSiteContext", "No site context in token. Call POST /auth/switch-site first.");
}
