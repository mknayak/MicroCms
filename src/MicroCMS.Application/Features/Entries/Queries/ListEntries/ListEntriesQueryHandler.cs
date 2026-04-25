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

/// <summary>Handles <see cref="ListEntriesQuery"/> with a cache-aside read pattern (Sprint 9).</summary>
public sealed class ListEntriesQueryHandler(
 IRepository<Entry, EntryId> entryRepository,
    ICacheService cacheService,
    ICurrentUser currentUser)
    : IRequestHandler<ListEntriesQuery, Result<PagedList<EntryListItemDto>>>
{
    public async Task<Result<PagedList<EntryListItemDto>>> Handle(
 ListEntriesQuery request,
  CancellationToken cancellationToken)
    {
     var tenantId = currentUser.TenantId;
        var cacheKey = CacheKeys.EntryList(tenantId, request.SiteId, request.StatusFilter, request.Page, request.PageSize);

        var cached = await cacheService.GetAsync<PagedList<EntryListItemDto>>(cacheKey, cancellationToken);
      if (cached is not null)
    return Result.Success(cached);

        var siteId = new SiteId(request.SiteId);

    var listSpec = new EntriesBySiteSpec(siteId, request.StatusFilter, request.Page, request.PageSize);
        var countSpec = new EntriesBySiteSpec(siteId, request.StatusFilter);

   var entries = await entryRepository.ListAsync(listSpec, cancellationToken);
      var totalCount = await entryRepository.CountAsync(countSpec, cancellationToken);

        var dtos = EntryMapper.ToListItemDtos(entries);
      var paged = PagedList<EntryListItemDto>.Create(dtos, request.Page, request.PageSize, totalCount);

  await cacheService.SetWithTagAsync(cacheKey, paged, CacheTags.TenantEntries(tenantId), cancellationToken: cancellationToken);

   return Result.Success(paged);
    }
}
