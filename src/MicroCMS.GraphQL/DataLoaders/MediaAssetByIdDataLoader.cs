using GreenDonut;
using MediatR;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Application.Features.Media.Queries;

namespace MicroCMS.GraphQL.DataLoaders;

/// <summary>
/// Batch DataLoader for <see cref="MediaAssetDto"/> — prevents N+1 queries
/// when a single request resolves many media assets by ID.
/// </summary>
public sealed class MediaAssetByIdDataLoader(IMediator mediator, IBatchScheduler scheduler, DataLoaderOptions options)
    : BatchDataLoader<Guid, MediaAssetDto?>(scheduler, options)
{
    protected override async Task<IReadOnlyDictionary<Guid, MediaAssetDto?>> LoadBatchAsync(
  IReadOnlyList<Guid> keys,
  CancellationToken cancellationToken)
    {
  var results = new Dictionary<Guid, MediaAssetDto?>(keys.Count);

        foreach (var id in keys)
        {
  var result = await mediator.Send(new GetMediaAssetQuery(id), cancellationToken);
         results[id] = result.IsSuccess ? result.Value : null;
        }

        return results;
    }
}
