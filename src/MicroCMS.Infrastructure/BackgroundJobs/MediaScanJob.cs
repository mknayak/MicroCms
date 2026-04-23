using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Media;
using MicroCMS.Shared.Ids;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace MicroCMS.Infrastructure.BackgroundJobs;

/// <summary>
/// Quartz job that runs on a configurable interval, picks up a batch of
/// <c>PendingScan</c> media assets, submits each to ClamAV, and transitions
/// the asset to <c>Available</c> or <c>Quarantined</c>.
///
/// Uses a scoped <see cref="IServiceScopeFactory"/> so EF Core DbContext and
/// repositories are created fresh per job execution.
/// </summary>
[DisallowConcurrentExecution]
public sealed class MediaScanJob : IJob
{
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MediaScanJob> _logger;

    public MediaScanJob(IServiceScopeFactory scopeFactory, ILogger<MediaScanJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var assetRepo = scope.ServiceProvider.GetRequiredService<IRepository<MediaAsset, MediaAssetId>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var scanner = scope.ServiceProvider.GetRequiredService<IClamAvScanner>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageProvider>();

        var pendingAssets = await assetRepo.ListAsync(
            new PendingScanAssetsSpec(BatchSize),
            context.CancellationToken);

        if (pendingAssets.Count == 0)
            return;

        _logger.LogInformation("MediaScanJob: scanning {Count} asset(s).", pendingAssets.Count);

        foreach (var asset in pendingAssets)
        {
            await ScanAssetAsync(asset, assetRepo, scanner, storage, context.CancellationToken);
        }

        await uow.SaveChangesAsync(context.CancellationToken);
    }

    private async Task ScanAssetAsync(
        MediaAsset asset,
        IRepository<MediaAsset, MediaAssetId> repo,
        IClamAvScanner scanner,
        IStorageProvider storage,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await storage.DownloadAsync(asset.StorageKey, cancellationToken);
            var result = await scanner.ScanAsync(stream, cancellationToken);

            if (result.IsClean)
            {
                asset.MarkAvailable();
                _logger.LogInformation("Asset {Id} ({FileName}) passed virus scan.", asset.Id, asset.Metadata.FileName);
            }
            else
            {
                asset.Quarantine(result.ThreatName ?? "Unknown threat");
                _logger.LogWarning("Asset {Id} ({FileName}) quarantined: {Threat}.",
                    asset.Id, asset.Metadata.FileName, result.ThreatName);
            }

            repo.Update(asset);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error scanning asset {Id}.", asset.Id);
        }
    }
}
