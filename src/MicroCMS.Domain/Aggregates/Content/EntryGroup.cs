using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Content;

/// <summary>
/// A named grouping of entries that all belong to the same <see cref="ContentType"/>.
/// Examples: "G7 Countries", "ASEAN Countries".
///
/// Members are stored as a collection of <see cref="EntryGroupMember"/> join records
/// so one entry can appear in multiple groups.
/// Image is stored as a nullable <see cref="MediaAssetId"/> reference.
/// </summary>
public sealed class EntryGroup : AggregateRoot<EntryGroupId>
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 1000;
    public const int MaxHandleLength = 64;

    private readonly List<EntryGroupMember> _members = [];

    private EntryGroup() : base() { } // EF Core

    private EntryGroup(
        EntryGroupId id,
        TenantId tenantId,
        SiteId siteId,
        ContentTypeId contentTypeId,
        string handle,
        string title,
        string? description,
        MediaAssetId? imageAssetId) : base(id)
    {
        TenantId = tenantId;
        SiteId = siteId;
        ContentTypeId = contentTypeId;
        Handle = handle;
        Title = title;
        Description = description;
        ImageAssetId = imageAssetId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }

    /// <summary>The content type whose entries this group organises.</summary>
    public ContentTypeId ContentTypeId { get; private set; }

    /// <summary>Machine-readable slug-style name, unique per site + content type.</summary>
    public string Handle { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>Optional hero image — references a <c>MediaAsset</c> by ID.</summary>
    public MediaAssetId? ImageAssetId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<EntryGroupMember> Members => _members.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────────────────

    public static EntryGroup Create(
        TenantId tenantId,
        SiteId siteId,
        ContentTypeId contentTypeId,
        string handle,
        string title,
        string? description = null,
        MediaAssetId? imageAssetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handle, nameof(handle));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        if (handle.Length > MaxHandleLength)
            throw new DomainException($"EntryGroup handle must not exceed {MaxHandleLength} characters.");

        if (title.Length > MaxTitleLength)
            throw new DomainException($"EntryGroup title must not exceed {MaxTitleLength} characters.");

        if (description?.Length > MaxDescriptionLength)
            throw new DomainException($"EntryGroup description must not exceed {MaxDescriptionLength} characters.");

        return new EntryGroup(
            EntryGroupId.New(),
            tenantId, siteId, contentTypeId,
            handle.Trim(), title.Trim(), description?.Trim(), imageAssetId);
    }

    // ── Mutation ───────────────────────────────────────────────────────────

    public void Update(string title, string? description, MediaAssetId? imageAssetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        if (title.Length > MaxTitleLength)
            throw new DomainException($"EntryGroup title must not exceed {MaxTitleLength} characters.");

        if (description?.Length > MaxDescriptionLength)
            throw new DomainException($"EntryGroup description must not exceed {MaxDescriptionLength} characters.");

        Title = title.Trim();
        Description = description?.Trim();
        ImageAssetId = imageAssetId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Replaces the full member list with <paramref name="entryIds"/>.</summary>
    public void SetMembers(IEnumerable<EntryId> entryIds)
    {
        _members.Clear();
        foreach (var entryId in entryIds.Distinct())
            _members.Add(new EntryGroupMember(Id, entryId));

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Adds a single entry if not already a member.</summary>
    public void AddMember(EntryId entryId)
    {
        if (_members.Any(m => m.EntryId == entryId))
            return;

        _members.Add(new EntryGroupMember(Id, entryId));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Removes a single entry from the group.</summary>
    public void RemoveMember(EntryId entryId)
    {
        var member = _members.FirstOrDefault(m => m.EntryId == entryId);
        if (member is not null)
        {
            _members.Remove(member);
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}

/// <summary>
/// Join record linking an <see cref="EntryGroup"/> to a member <see cref="Entry"/>.
/// Owned by the <see cref="EntryGroup"/> aggregate (no independent lifecycle).
/// </summary>
public sealed class EntryGroupMember
{
    private EntryGroupMember() { } // EF Core

    internal EntryGroupMember(EntryGroupId groupId, EntryId entryId)
    {
        GroupId = groupId;
        EntryId = entryId;
    }

    public EntryGroupId GroupId { get; private set; }
    public EntryId EntryId { get; private set; }
}
