using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Application.Features.Search.EventHandlers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Queries.GetEntry;

/// <summary>Handles <see cref="GetEntryQuery"/> with a cache-aside read pattern (Sprint 9).</summary>
public sealed class GetEntryQueryHandler(
    IRepository<Entry, EntryId> entryRepository,
    ICacheService cacheService,
    ICurrentUser currentUser)
    : IRequestHandler<GetEntryQuery, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(
        GetEntryQuery request,
     CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
     var entryId = new EntryId(request.EntryId);
        var cacheKey = CacheKeys.Entry(tenantId, entryId);

    var cached = await cacheService.GetAsync<EntryDto>(cacheKey, cancellationToken);
        if (cached is not null)
       return Result.Success(cached);

  var entry = await entryRepository.GetByIdAsync(entryId, cancellationToken)
   ?? throw new NotFoundException(nameof(Entry), entryId);

 // Fetch locale variants for the same slug+contentType
        var variantsSpec = new EntryLocaleVariantsSpec(entry.SiteId, entry.ContentTypeId, entry.Slug);
        var variants = await entryRepository.ListAsync(variantsSpec, cancellationToken);
  var localeVariants = variants.Select(e => e.Locale.Value).ToList().AsReadOnly();

        var dto = EntryMapper.ToDto(entry, localeVariants);

        await cacheService.SetWithTagAsync(cacheKey, dto, CacheTags.TenantEntries(tenantId), cancellationToken: cancellationToken);

        return Result.Success(dto);
    }
}
