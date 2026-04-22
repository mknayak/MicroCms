using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Events.Media;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Media;

/// <summary>
/// Media asset aggregate root. Tracks a file through its full lifecycle:
/// Uploading → PendingScan → Available (or Quarantined) → Deleted.
/// Storage location is intentionally kept as a provider-neutral string key
/// so the Infrastructure layer can interpret it as a local path, S3 key, etc.
/// </summary>
public sealed class MediaAsset : AggregateRoot<MediaAssetId>
{
    private readonly List<string> _tags = [];

    private MediaAsset() : base() { } // EF Core

    private MediaAsset(
        MediaAssetId id,
      TenantId tenantId,
  SiteId siteId,
        AssetMetadata metadata,
  string storageKey,
        Guid? folderId,
        Guid uploadedBy) : base(id)
    {
  TenantId = tenantId;
   SiteId = siteId;
  Metadata = metadata;
    StorageKey = storageKey;
        FolderId = folderId;
     UploadedBy = uploadedBy;
   Status = MediaAssetStatus.Uploading;
        CreatedAt = DateTimeOffset.UtcNow;
 UpdatedAt = DateTimeOffset.UtcNow;
    }

public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public AssetMetadata Metadata { get; private set; } = null!;

    /// <summary>
    /// Provider-neutral storage key (e.g. S3 object key, blob name, relative file path).
    /// Opaque to the domain; interpreted only by the storage adapter.
    /// </summary>
    public string StorageKey { get; private set; } = string.Empty;

    public Guid? FolderId { get; private set; }
    public Guid UploadedBy { get; private set; }
    public MediaAssetStatus Status { get; private set; }
    public string? AltText { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────────────────

    public static MediaAsset Create(
        TenantId tenantId,
        SiteId siteId,
        AssetMetadata metadata,
        string storageKey,
        Guid uploadedBy,
        Guid? folderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey, nameof(storageKey));

        var asset = new MediaAsset(
            MediaAssetId.New(), tenantId, siteId,
            metadata, storageKey, folderId, uploadedBy);

        asset.RaiseDomainEvent(new MediaAssetUploadStartedEvent(asset.Id, tenantId, metadata.FileName));
        return asset;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public void MarkUploadComplete()
    {
        EnsureStatus(MediaAssetStatus.Uploading, "MarkUploadComplete");
        Status = MediaAssetStatus.PendingScan;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAvailable()
    {
        EnsureStatus(MediaAssetStatus.PendingScan, "MarkAvailable");
        Status = MediaAssetStatus.Available;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new MediaAssetAvailableEvent(Id, TenantId, SiteId, Metadata.FileName));
    }

    public void Quarantine(string scanResult)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scanResult, nameof(scanResult));
        EnsureStatus(MediaAssetStatus.PendingScan, "Quarantine");
        Status = MediaAssetStatus.Quarantined;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new MediaAssetQuarantinedEvent(Id, TenantId, scanResult));
    }

    public void Delete(Guid deletedBy)
    {
        if (Status == MediaAssetStatus.Deleted)
        {
            throw new BusinessRuleViolationException("MediaAsset.AlreadyDeleted", "Asset is already deleted.");
        }

        Status = MediaAssetStatus.Deleted;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new MediaAssetDeletedEvent(Id, TenantId, deletedBy));
    }

    // ── Metadata updates ─────────────────────────────────────────────────

    public void UpdateAltText(string? altText)
    {
        EnsureAvailable();
        AltText = altText?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetTags(IEnumerable<string> tags)
    {
        EnsureAvailable();
        _tags.Clear();
        _tags.AddRange(tags.Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).Distinct());
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MoveToFolder(Guid? folderId)
    {
        EnsureAvailable();
        FolderId = folderId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void EnsureAvailable()
    {
        if (Status != MediaAssetStatus.Available)
        {
            throw new BusinessRuleViolationException(
                "MediaAsset.NotAvailable",
                $"Operation requires asset to be Available. Current status: {Status}.");
        }
    }

    private void EnsureStatus(MediaAssetStatus required, string operation)
    {
        if (Status != required)
        {
            throw new InvalidStateTransitionException(
                "MediaAsset", Status.ToString(), operation);
        }
    }
}
