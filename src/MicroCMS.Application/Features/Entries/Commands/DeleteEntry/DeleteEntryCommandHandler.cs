using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Commands.DeleteEntry;

/// <summary>
/// Handles <see cref="DeleteEntryCommand"/>.
/// Guards against deleting a Published entry — callers must Unpublish first.
/// </summary>
public sealed class DeleteEntryCommandHandler(
    IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<DeleteEntryCommand, Result>
{
    public async Task<Result> Handle(
        DeleteEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entryId = new EntryId(request.EntryId);
        var entry = await entryRepository.GetByIdAsync(entryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), entryId);

        if (entry.Status == EntryStatus.Published)
        {
            return Result.Failure(Error.Conflict(
                "Entry.CannotDeletePublished",
                "Published entries must be unpublished before deletion."));
        }

        entryRepository.Remove(entry);
        return Result.Success();
    }
}
