using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Events.Content;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Content;

/// <summary>
/// Entry aggregate root. Represents a single piece of content conforming to a <see cref="ContentType"/>.
/// Manages the full lifecycle: Draft → PendingApproval → Approved → Published / Unpublished.
/// Supports versioning (FR-CM-5), localisation (FR-CM-8), and scheduling (FR-CM-4).
/// </summary>
public sealed class Entry : AggregateRoot<EntryId>
{
    private readonly List<EntryVersion> _versions = [];

    private Entry() : base() { } // EF Core

    private Entry(
        EntryId id,
        TenantId tenantId,
        SiteId siteId,
        ContentTypeId contentTypeId,
        Slug slug,
        Locale locale,
        Guid authorId) : base(id)
    {
        TenantId = tenantId;
        SiteId = siteId;
        ContentTypeId = contentTypeId;
        Slug = slug;
        Locale = locale;
        AuthorId = authorId;
        Status = EntryStatus.Draft;
        CurrentVersionNumber = 0;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        Seo = SeoMetadata.Empty;
    }

    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public ContentTypeId ContentTypeId { get; private set; }
    public Slug Slug { get; private set; } = null!;
    public Locale Locale { get; private set; } = null!;
    public Guid AuthorId { get; private set; }
    public EntryStatus Status { get; private set; }
    public int CurrentVersionNumber { get; private set; }
    public string FieldsJson { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? ScheduledPublishAt { get; private set; }
    public DateTimeOffset? ScheduledUnpublishAt { get; private set; }
    public IReadOnlyList<EntryVersion> Versions => _versions.AsReadOnly();
    public SeoMetadata Seo { get; private set; } = null!;
    public FolderId? FolderId { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────

    public static Entry Create(
        TenantId tenantId,
        SiteId siteId,
        ContentTypeId contentTypeId,
        Slug slug,
        Locale locale,
        Guid authorId,
        string fieldsJson = "{}",
        FolderId? folderId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldsJson, nameof(fieldsJson));

        var entry = new Entry(EntryId.New(), tenantId, siteId, contentTypeId, slug, locale, authorId)
        {
            FieldsJson = fieldsJson,
            FolderId = folderId
        };

        entry.SnapshotVersion(authorId, changeNote: "Initial draft");
        entry.RaiseDomainEvent(new EntryCreatedEvent(entry.Id, tenantId, siteId, contentTypeId, slug.Value, locale.Value));
        return entry;
    }

    // ── Editing ───────────────────────────────────────────────────────────

    public void UpdateFields(string fieldsJson, Guid editorId, string? changeNote = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldsJson, nameof(fieldsJson));
        EnsureEditable();

        FieldsJson = fieldsJson;
        UpdatedAt = DateTimeOffset.UtcNow;
        SnapshotVersion(editorId, changeNote);
        RaiseDomainEvent(new EntryUpdatedEvent(Id, TenantId, editorId));
    }

    public void UpdateSlug(Slug newSlug)
    {
        EnsureEditable();
        Slug = newSlug;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Updates SEO metadata fields on the entry (GAP-08).</summary>
    public void UpdateSeoMetadata(SeoMetadata seo)
    {
        ArgumentNullException.ThrowIfNull(seo, nameof(seo));
        Seo = seo;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Moves the entry into a folder or to the site root (GAP-02).</summary>
    public void MoveToFolder(FolderId? folderId)
    {
        FolderId = folderId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Workflow transitions ───────────────────────────────────────────────

    public void Submit()
    {
        EnsureStatus(EntryStatus.Draft, "Submit");
        Status = EntryStatus.PendingApproval;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Approve()
    {
        EnsureStatus(EntryStatus.PendingApproval, "Approve");
        Status = EntryStatus.Approved;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReturnToDraft(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        if (Status != EntryStatus.PendingApproval)
        {
            throw new InvalidStateTransitionException("Entry", Status.ToString(), "Draft");
        }

        Status = EntryStatus.Draft;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Publish()
    {
        if (Status != EntryStatus.Approved && Status != EntryStatus.Scheduled)
        {
            throw new BusinessRuleViolationException(
                "Entry.CannotPublishWithoutApproval",
                $"Entry must be Approved or Scheduled before publishing. Current status: {Status}.");
        }

        Status = EntryStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        ScheduledPublishAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new EntryPublishedEvent(Id, TenantId, SiteId, Slug.Value, Locale.Value));
    }

    public void Unpublish()
    {
        EnsureStatus(EntryStatus.Published, "Unpublish");
        Status = EntryStatus.Unpublished;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new EntryUnpublishedEvent(Id, TenantId));
    }

    public void Archive()
    {
        if (Status == EntryStatus.Archived)
        {
            throw new BusinessRuleViolationException("Entry.AlreadyArchived", "Entry is already archived.");
        }

        Status = EntryStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new EntryArchivedEvent(Id, TenantId));
    }

    public void SchedulePublish(DateTimeOffset publishAt, DateTimeOffset? unpublishAt = null)
    {
        if (Status != EntryStatus.Approved)
        {
            throw new BusinessRuleViolationException(
                "Entry.CannotScheduleWithoutApproval",
                "Entry must be Approved before scheduling publish.");
        }

        if (publishAt <= DateTimeOffset.UtcNow)
        {
            throw new DomainException("Scheduled publish time must be in the future.");
        }

        if (unpublishAt.HasValue && unpublishAt.Value <= publishAt)
        {
            throw new DomainException("Scheduled unpublish time must be after publish time.");
        }

        ScheduledPublishAt = publishAt;
        ScheduledUnpublishAt = unpublishAt;
        Status = EntryStatus.Scheduled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CancelScheduledPublish()
    {
        if (Status != EntryStatus.Scheduled)
        {
            throw new InvalidStateTransitionException("Entry", Status.ToString(), "CancelScheduledPublish");
        }

        ScheduledPublishAt = null;
        ScheduledUnpublishAt = null;
        Status = EntryStatus.Approved;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RollbackToVersion(int versionNumber, Guid editorId)
    {
        EnsureEditable();

        var version = _versions.FirstOrDefault(v => v.VersionNumber == versionNumber)
            ?? throw new DomainException($"Version {versionNumber} not found.");

        FieldsJson = version.FieldsJson;
        UpdatedAt = DateTimeOffset.UtcNow;
        SnapshotVersion(editorId, $"Rolled back to v{versionNumber}");
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void EnsureEditable()
    {
        if (Status is EntryStatus.Archived or EntryStatus.Published)
        {
            throw new BusinessRuleViolationException(
                "Entry.NotEditable",
                $"Entry cannot be edited while in '{Status}' status.");
        }
    }

    private void EnsureStatus(EntryStatus required, string operation)
    {
        if (Status != required)
        {
            throw new InvalidStateTransitionException("Entry", Status.ToString(), operation);
        }
    }

    private void SnapshotVersion(Guid authorId, string? changeNote)
    {
        CurrentVersionNumber++;
        _versions.Add(new EntryVersion(Id, CurrentVersionNumber, FieldsJson, authorId, changeNote));
    }
}
