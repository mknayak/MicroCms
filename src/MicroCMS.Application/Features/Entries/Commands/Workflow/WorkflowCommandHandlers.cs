using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Commands.Workflow;

/// <summary>Handles <see cref="SubmitForReviewCommand"/>.</summary>
public sealed class SubmitForReviewCommandHandler(
    IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<SubmitForReviewCommand, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(SubmitForReviewCommand request, CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdAsync(new EntryId(request.EntryId), cancellationToken)
 ?? throw new NotFoundException(nameof(Entry), request.EntryId);
        entry.Submit();
        entryRepository.Update(entry);
   return Result.Success(EntryMapper.ToDto(entry));
    }
}

/// <summary>Handles <see cref="ApproveEntryCommand"/>.</summary>
public sealed class ApproveEntryCommandHandler(
    IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<ApproveEntryCommand, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(ApproveEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdAsync(new EntryId(request.EntryId), cancellationToken)
 ?? throw new NotFoundException(nameof(Entry), request.EntryId);
    entry.Approve();
    entryRepository.Update(entry);
  return Result.Success(EntryMapper.ToDto(entry));
    }
}

/// <summary>Handles <see cref="RejectEntryCommand"/>.</summary>
public sealed class RejectEntryCommandHandler(
    IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<RejectEntryCommand, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(RejectEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdAsync(new EntryId(request.EntryId), cancellationToken)
       ?? throw new NotFoundException(nameof(Entry), request.EntryId);
   entry.ReturnToDraft(request.Reason);
        entryRepository.Update(entry);
        return Result.Success(EntryMapper.ToDto(entry));
    }
}
