using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Media.Commands;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Media.Handlers;

/// <summary>Moves each asset in the batch to the specified folder (or root when null).</summary>
public sealed class BulkMoveMediaCommandHandler(
    IRepository<MediaAsset, MediaAssetId> repo,
    ICurrentUser currentUser) : IRequestHandler<BulkMoveMediaCommand, Result>
{
    public async Task<Result> Handle(BulkMoveMediaCommand request, CancellationToken cancellationToken)
    {
        foreach (var id in request.AssetIds)
        {
            var asset = await repo.GetByIdAsync(new MediaAssetId(id), cancellationToken)
                ?? throw new NotFoundException(nameof(MediaAsset), id);

            EnsureSameTenant(asset, currentUser);
            asset.MoveToFolder(request.TargetFolderId);
            repo.Update(asset);
        }

        return Result.Success();
    }

    private static void EnsureSameTenant(MediaAsset asset, ICurrentUser user)
    {
        if (asset.TenantId != user.TenantId)
            throw new ForbiddenException("Asset does not belong to your tenant.");
    }
}

/// <summary>Soft-deletes each asset in the batch.</summary>
public sealed class BulkDeleteMediaCommandHandler(
    IRepository<MediaAsset, MediaAssetId> repo,
    ICurrentUser currentUser) : IRequestHandler<BulkDeleteMediaCommand, Result>
{
    public async Task<Result> Handle(BulkDeleteMediaCommand request, CancellationToken cancellationToken)
    {
        foreach (var id in request.AssetIds)
        {
            var asset = await repo.GetByIdAsync(new MediaAssetId(id), cancellationToken)
                ?? throw new NotFoundException(nameof(MediaAsset), id);

            if (asset.TenantId != currentUser.TenantId)
                throw new ForbiddenException("Asset does not belong to your tenant.");

            asset.Delete(currentUser.UserId);
            repo.Update(asset);
        }

        return Result.Success();
    }
}

/// <summary>Replaces the tag list on every asset in the batch.</summary>
public sealed class BulkRetagMediaCommandHandler(
    IRepository<MediaAsset, MediaAssetId> repo,
    ICurrentUser currentUser) : IRequestHandler<BulkRetagMediaCommand, Result>
{
    public async Task<Result> Handle(BulkRetagMediaCommand request, CancellationToken cancellationToken)
    {
        foreach (var id in request.AssetIds)
        {
            var asset = await repo.GetByIdAsync(new MediaAssetId(id), cancellationToken)
                ?? throw new NotFoundException(nameof(MediaAsset), id);

            if (asset.TenantId != currentUser.TenantId)
                throw new ForbiddenException("Asset does not belong to your tenant.");

            asset.SetTags(request.Tags);
            repo.Update(asset);
        }

        return Result.Success();
    }
}
