using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Media.Commands;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Services;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Media.Handlers;

/// <summary>
/// Generates a time-limited signed delivery URL for a private media asset.
/// The signed URL embeds the tenant scope in its HMAC payload so cross-tenant
/// URL reuse is rejected by the signing service.
/// </summary>
internal sealed class GetSignedUrlCommandHandler(
    IRepository<MediaAsset, MediaAssetId> repo,
    IStorageSigningService signingService,
    ICurrentUser currentUser) : IRequestHandler<GetSignedUrlCommand, Result<SignedUrlDto>>
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(1);

    public async Task<Result<SignedUrlDto>> Handle(
        GetSignedUrlCommand request,
        CancellationToken cancellationToken)
    {
        var asset = await repo.GetByIdAsync(new MediaAssetId(request.AssetId), cancellationToken)
            ?? throw new NotFoundException(nameof(MediaAsset), request.AssetId);

        if (asset.TenantId != currentUser.TenantId)
        {
            return Result.Failure<SignedUrlDto>(
                Error.Forbidden("Media.CrossTenantAccess", "Asset does not belong to your tenant."));
        }

        var expiry = request.ExpiresIn ?? DefaultExpiry;
        var url = await signingService.GenerateSignedUrlAsync(asset.StorageKey, expiry, cancellationToken);

        return Result.Success(new SignedUrlDto(
            asset.Id.Value,
            url,
            DateTimeOffset.UtcNow.Add(expiry)));
    }
}
