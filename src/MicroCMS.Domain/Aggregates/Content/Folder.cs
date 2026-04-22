using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Content;

/// <summary>
/// A named container for grouping entries within a site (GAP-02).
/// Folders can be nested via <see cref="ParentFolderId"/>.
/// Maximum nesting depth is enforced at the application layer to keep cyclomatic complexity low.
/// </summary>
public sealed class Folder : AggregateRoot<FolderId>
{
    public const int MaxNameLength = 200;
 public const int MaxDepth = 10;

    private Folder() : base() { } // EF Core

    private Folder(FolderId id, TenantId tenantId, SiteId siteId, string name, FolderId? parentFolderId)
        : base(id)
    {
        TenantId = tenantId;
        SiteId = siteId;
     Name = name;
   ParentFolderId = parentFolderId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

  public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public FolderId? ParentFolderId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────

    public static Folder Create(
        TenantId tenantId,
        SiteId siteId,
        string name,
    FolderId? parentFolderId = null)
    {
 ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (name.Length > MaxNameLength)
       throw new DomainException($"Folder name must not exceed {MaxNameLength} characters.");

        return new Folder(FolderId.New(), tenantId, siteId, name.Trim(), parentFolderId);
    }

 // ── Mutations ─────────────────────────────────────────────────────────

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName, nameof(newName));

        if (newName.Length > MaxNameLength)
    throw new DomainException($"Folder name must not exceed {MaxNameLength} characters.");

        Name = newName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MoveTo(FolderId? newParentFolderId)
    {
        if (newParentFolderId == Id)
            throw new BusinessRuleViolationException("Folder.CircularReference", "A folder cannot be its own parent.");

        ParentFolderId = newParentFolderId;
     UpdatedAt = DateTimeOffset.UtcNow;
    }
}
