using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Commands.PublishEntry;

/// <summary>Handles <see cref="PublishEntryCommand"/>.</summary>
public sealed class PublishEntryCommandHandler(
    IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<PublishEntryCommand, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(
        PublishEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entryId = new EntryId(request.EntryId);
        var entry = await entryRepository.GetByIdAsync(entryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), entryId);

        entry.Publish();
        entryRepository.Update(entry);

        return Result.Success(EntryMapper.ToDto(entry));
    }
}
