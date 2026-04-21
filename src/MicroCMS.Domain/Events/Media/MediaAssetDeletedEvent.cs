using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Media;

public sealed record MediaAssetDeletedEvent(
    MediaAssetId AssetId,
    TenantId TenantId,
    Guid DeletedBy) : DomainEvent;
