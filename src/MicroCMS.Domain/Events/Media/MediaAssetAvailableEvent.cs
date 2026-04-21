using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Media;

public sealed record MediaAssetAvailableEvent(
    MediaAssetId AssetId,
    TenantId TenantId,
    SiteId SiteId,
    string FileName) : DomainEvent;
