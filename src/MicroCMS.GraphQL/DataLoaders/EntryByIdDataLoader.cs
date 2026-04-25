using GreenDonut;
using MediatR;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Queries.GetEntry;

namespace MicroCMS.GraphQL.DataLoaders;

/// <summary>
/// Batch DataLoader for <see cref="EntryDto"/> — prevents N+1 queries when
/// a single GraphQL request resolves many entries by ID (e.g. nested references).
/// </summary>
public sealed class EntryByIdDataLoader(IMediator mediator, IBatchScheduler scheduler, DataLoaderOptions options)
    : BatchDataLoader<Guid, EntryDto?>(scheduler, options)
{
    protected override async Task<IReadOnlyDictionary<Guid, EntryDto?>> LoadBatchAsync(
   IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
  var results = new Dictionary<Guid, EntryDto?>(keys.Count);

        // Issue individual handler calls — a future optimization would accept a list query,
        // but for now the MediatR pipeline validates auth per-query which is correct.
        foreach (var id in keys)
        {
     var result = await mediator.Send(new GetEntryQuery(id), cancellationToken);
       results[id] = result.IsSuccess ? result.Value : null;
        }

  return results;
    }
}
