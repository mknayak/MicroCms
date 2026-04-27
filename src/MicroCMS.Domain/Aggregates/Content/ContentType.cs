using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Events.Content;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Content;

/// <summary>
/// Content type aggregate root. Defines the schema (set of fields) for entries.
/// Handle is the machine-readable API name; displayName is shown in the admin UI.
/// </summary>
public sealed class ContentType : AggregateRoot<ContentTypeId>
{
    private readonly List<FieldDefinition> _fields = [];

    private ContentType() : base() { } // EF Core

    private ContentType(
     ContentTypeId id,
        TenantId tenantId,
  SiteId siteId,
        string handle,
        string displayName,
  string? description) : base(id)
  {
        TenantId = tenantId;
        SiteId = siteId;
   Handle = handle;
     DisplayName = displayName;
        Description = description;
        Status = ContentTypeStatus.Draft;
   CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public string Handle { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ContentTypeStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
  public IReadOnlyList<FieldDefinition> Fields => _fields.AsReadOnly();

    public const int MaxHandleLength = 64;
    public const int MaxDisplayNameLength = 200;
    public const int MaxDescriptionLength = 500;

    // ── Factory ────────────────────────────────────────────────────────────

    public static ContentType Create(
        TenantId tenantId,
        SiteId siteId,
        string handle,
        string displayName,
        string? description = null)
    {
        ValidateHandle(handle);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));

        if (displayName.Length > MaxDisplayNameLength)
        {
            throw new DomainException($"Display name must not exceed {MaxDisplayNameLength} characters.");
        }

        var ct = new ContentType(
            ContentTypeId.New(), tenantId, siteId,
            handle.Trim(), displayName.Trim(), description?.Trim());

        ct.RaiseDomainEvent(new ContentTypeCreatedEvent(ct.Id, ct.TenantId, handle));
        return ct;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public void Update(string displayName, string? description)
    {
    EnsureNotArchived();
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));
  if (displayName.Length > MaxDisplayNameLength)
            throw new DomainException($"Display name must not exceed {MaxDisplayNameLength} characters.");

        DisplayName = displayName.Trim();
        Description = description?.Trim();
     UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Publish()
    {
        if (Status != ContentTypeStatus.Draft)
        {
            throw new InvalidStateTransitionException("ContentType", Status.ToString(), "Active");
        }

        if (_fields.Count == 0)
        {
            throw new BusinessRuleViolationException(
                "ContentType.NoFields",
                "A content type must have at least one field before it can be published.");
        }

        Status = ContentTypeStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        if (Status == ContentTypeStatus.Archived)
        {
            throw new BusinessRuleViolationException(
                "ContentType.AlreadyArchived",
                "Content type is already archived.");
        }

        Status = ContentTypeStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Fields ────────────────────────────────────────────────────────────

    public FieldDefinition AddField(
        string handle,
        string label,
        FieldType fieldType,
        bool isRequired = false,
        bool isLocalized = false,
        bool isUnique = false,
        string? description = null,
        string? validationJson = null)
    {
        EnsureNotArchived();

        if (_fields.Exists(f => f.Handle.Equals(handle.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessRuleViolationException(
                "ContentType.DuplicateFieldHandle",
                $"A field with handle '{handle}' already exists.");
        }

        var field = FieldDefinition.Create(
            Id, handle, label, fieldType,
            isRequired, isLocalized, isUnique,
            sortOrder: _fields.Count,
            description, validationJson);

        _fields.Add(field);
        UpdatedAt = DateTimeOffset.UtcNow;
        return field;
    }

    public void UpdateField(
        Guid fieldId,
        string label,
        FieldType fieldType,
        bool isRequired,
        bool isLocalized,
        int sortOrder,
        string? description)
    {
        EnsureNotArchived();
        var field = GetFieldOrThrow(fieldId);
        field.Update(label, fieldType, isRequired, isLocalized, sortOrder, description);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveField(Guid fieldId)
    {
        EnsureNotArchived();
        var field = GetFieldOrThrow(fieldId);
        _fields.Remove(field);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void EnsureNotArchived()
    {
        if (Status == ContentTypeStatus.Archived)
        {
            throw new BusinessRuleViolationException(
                "ContentType.IsArchived",
                "Cannot modify an archived content type.");
        }
    }

    private FieldDefinition GetFieldOrThrow(Guid fieldId) =>
        _fields.FirstOrDefault(f => f.Id == fieldId)
        ?? throw new DomainException($"Field '{fieldId}' not found on content type '{Handle}'.");

    private static void ValidateHandle(string handle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handle, nameof(handle));

        if (handle.Length > MaxHandleLength)
        {
            throw new DomainException($"ContentType handle must not exceed {MaxHandleLength} characters.");
        }

        if (!handle.All(c => char.IsLetterOrDigit(c) || c == '_'))
        {
            throw new DomainException(
                "ContentType handle may only contain letters, digits, and underscores.");
        }
    }
}
