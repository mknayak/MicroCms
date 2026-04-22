using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Commands.UnpublishEntry;

/// <summary>Handles <see cref="UnpublishEntryCommand"/>.</summary>
public sealed class UnpublishEntryCommandHandler(
    IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<UnpublishEntryCommand, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(
        UnpublishEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entryId = new EntryId(request.EntryId);
        var entry = await entryRepository.GetByIdAsync(entryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), entryId);

        entry.Unpublish();
        entryRepository.Update(entry);

        return Result.Success(EntryMapper.ToDto(entry));
    }
}
