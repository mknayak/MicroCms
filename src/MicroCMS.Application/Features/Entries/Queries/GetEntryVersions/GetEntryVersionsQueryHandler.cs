using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Queries.GetEntryVersions;

/// <summary>Handles <see cref="GetEntryVersionsQuery"/>.</summary>
public sealed class GetEntryVersionsQueryHandler(
    IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<GetEntryVersionsQuery, Result<IReadOnlyList<EntryVersionDto>>>
{
    public async Task<Result<IReadOnlyList<EntryVersionDto>>> Handle(
        GetEntryVersionsQuery request,
        CancellationToken cancellationToken)
    {
        var entryId = new EntryId(request.EntryId);
        var entry = await entryRepository.GetByIdAsync(entryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), entryId);

        var versionDtos = EntryMapper.ToVersionDtos(
            entry.Versions.OrderByDescending(v => v.VersionNumber));

        return Result.Success(versionDtos);
    }
}
