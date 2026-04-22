using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Commands.CancelScheduledPublish;

/// <summary>Handles <see cref="CancelScheduledPublishCommand"/>.</summary>
public sealed class CancelScheduledPublishCommandHandler(
    IRepository<Entry, EntryId> entryRepository)
  : IRequestHandler<CancelScheduledPublishCommand, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(
        CancelScheduledPublishCommand request,
        CancellationToken cancellationToken)
    {
 var entryId = new EntryId(request.EntryId);
   var entry = await entryRepository.GetByIdAsync(entryId, cancellationToken)
  ?? throw new NotFoundException(nameof(Entry), entryId);

        entry.CancelScheduledPublish();
        entryRepository.Update(entry);

   return Result.Success(EntryMapper.ToDto(entry));
  }
}
