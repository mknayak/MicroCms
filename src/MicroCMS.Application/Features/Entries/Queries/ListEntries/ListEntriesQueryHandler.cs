using MediatR;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Queries.ListEntries;

/// <summary>Handles <see cref="ListEntriesQuery"/>.</summary>
public sealed class ListEntriesQueryHandler(
    IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<ListEntriesQuery, Result<PagedList<EntryListItemDto>>>
{
    public async Task<Result<PagedList<EntryListItemDto>>> Handle(
        ListEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);

        var listSpec = new EntriesBySiteSpec(siteId, request.StatusFilter, request.Page, request.PageSize);
        var countSpec = new EntriesBySiteSpec(siteId, request.StatusFilter);

        var entries = await entryRepository.ListAsync(listSpec, cancellationToken);
        var totalCount = await entryRepository.CountAsync(countSpec, cancellationToken);

        var dtos = EntryMapper.ToListItemDtos(entries);
        var paged = PagedList<EntryListItemDto>.Create(dtos, request.Page, request.PageSize, totalCount);

        return Result.Success(paged);
    }
}
