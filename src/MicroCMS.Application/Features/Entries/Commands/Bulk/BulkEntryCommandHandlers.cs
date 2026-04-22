using MediatR;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Commands.Bulk;

/// <summary>Handles <see cref="BulkPublishEntriesCommand"/>.</summary>
public sealed class BulkPublishEntriesCommandHandler(IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<BulkPublishEntriesCommand, Result<BulkOperationResult>>
{
    public async Task<Result<BulkOperationResult>> Handle(
     BulkPublishEntriesCommand request, CancellationToken cancellationToken)
    {
        var succeeded = new List<Guid>();
        var failed = new List<BulkOperationFailure>();

        foreach (var id in request.EntryIds)
     await ProcessOneAsync(id, succeeded, failed, entryRepository, cancellationToken);

   return Result.Success(new BulkOperationResult(succeeded, failed));
 }

    private static async Task ProcessOneAsync(
        Guid id, List<Guid> succeeded, List<BulkOperationFailure> failed,
     IRepository<Entry, EntryId> repo, CancellationToken ct)
    {
  var entry = await repo.GetByIdAsync(new EntryId(id), ct);
        if (entry is null) { failed.Add(new(id, "Not found.")); return; }

        try
        {
           entry.Publish();
        repo.Update(entry);
     succeeded.Add(id);
    }
        catch (Exception ex)
        {
    failed.Add(new(id, ex.Message));
        }
    }
}

/// <summary>Handles <see cref="BulkUnpublishEntriesCommand"/>.</summary>
public sealed class BulkUnpublishEntriesCommandHandler(IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<BulkUnpublishEntriesCommand, Result<BulkOperationResult>>
{
    public async Task<Result<BulkOperationResult>> Handle(
        BulkUnpublishEntriesCommand request, CancellationToken cancellationToken)
  {
     var succeeded = new List<Guid>();
      var failed = new List<BulkOperationFailure>();

 foreach (var id in request.EntryIds)
      await ProcessOneAsync(id, succeeded, failed, entryRepository, cancellationToken);

        return Result.Success(new BulkOperationResult(succeeded, failed));
    }

    private static async Task ProcessOneAsync(
 Guid id, List<Guid> succeeded, List<BulkOperationFailure> failed,
      IRepository<Entry, EntryId> repo, CancellationToken ct)
    {
      var entry = await repo.GetByIdAsync(new EntryId(id), ct);
        if (entry is null) { failed.Add(new(id, "Not found.")); return; }

      try
     {
   entry.Unpublish();
      repo.Update(entry);
   succeeded.Add(id);
        }
        catch (Exception ex)
        {
   failed.Add(new(id, ex.Message));
     }
    }
}

/// <summary>Handles <see cref="BulkDeleteEntriesCommand"/>.</summary>
public sealed class BulkDeleteEntriesCommandHandler(IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<BulkDeleteEntriesCommand, Result<BulkOperationResult>>
{
    public async Task<Result<BulkOperationResult>> Handle(
     BulkDeleteEntriesCommand request, CancellationToken cancellationToken)
    {
        var succeeded = new List<Guid>();
   var failed = new List<BulkOperationFailure>();

  foreach (var id in request.EntryIds)
            await ProcessOneAsync(id, succeeded, failed, entryRepository, cancellationToken);

      return Result.Success(new BulkOperationResult(succeeded, failed));
    }

  private static async Task ProcessOneAsync(
  Guid id, List<Guid> succeeded, List<BulkOperationFailure> failed,
        IRepository<Entry, EntryId> repo, CancellationToken ct)
    {
        var entry = await repo.GetByIdAsync(new EntryId(id), ct);
        if (entry is null) { failed.Add(new(id, "Not found.")); return; }

        if (entry.Status == EntryStatus.Published)
  {
            failed.Add(new(id, "Published entries must be unpublished before deletion."));
            return;
      }

  repo.Remove(entry);
 succeeded.Add(id);
    }
}
