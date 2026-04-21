using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Media;

public sealed record MediaAssetUploadStartedEvent(
    MediaAssetId AssetId,
    TenantId TenantId,
    string FileName) : DomainEvent;
