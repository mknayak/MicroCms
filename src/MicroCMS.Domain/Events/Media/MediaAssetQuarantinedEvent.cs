using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Media;

public sealed record MediaAssetQuarantinedEvent(
    MediaAssetId AssetId,
    TenantId TenantId,
    string ScanResult) : DomainEvent;
