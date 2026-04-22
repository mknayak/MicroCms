using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Commands.RollbackEntryVersion;

/// <summary>Handles <see cref="RollbackEntryVersionCommand"/>.</summary>
public sealed class RollbackEntryVersionCommandHandler(
    IRepository<Entry, EntryId> entryRepository,
    ICurrentUser currentUser)
    : IRequestHandler<RollbackEntryVersionCommand, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(
        RollbackEntryVersionCommand request,
        CancellationToken cancellationToken)
    {
        var entryId = new EntryId(request.EntryId);
        var entry = await entryRepository.GetByIdAsync(entryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), entryId);

        entry.RollbackToVersion(request.TargetVersionNumber, currentUser.UserId);
        entryRepository.Update(entry);

        return Result.Success(EntryMapper.ToDto(entry));
    }
}
