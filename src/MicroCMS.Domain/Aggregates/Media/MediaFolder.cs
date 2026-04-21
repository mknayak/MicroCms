using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Media;

/// <summary>
/// Represents a folder in the media library hierarchy (FR-ML-5).
/// Supports a tree structure via an optional parent folder.
/// </summary>
public sealed class MediaFolder : Entity<Guid>
{
    public const int MaxNameLength = 200;

    private MediaFolder() { } // EF Core

    internal MediaFolder(
        Guid id,
        TenantId tenantId,
        SiteId siteId,
        string name,
        Guid? parentFolderId)
        : base(id)
    {
        TenantId = tenantId;
        SiteId = siteId;
        Name = name;
        ParentFolderId = parentFolderId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentFolderId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    internal void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName, nameof(newName));

        if (newName.Length > MaxNameLength)
        {
            throw new DomainException($"Folder name must not exceed {MaxNameLength} characters.");
        }

        Name = newName.Trim();
    }

    internal void Move(Guid? newParentFolderId) =>
        ParentFolderId = newParentFolderId;
}
