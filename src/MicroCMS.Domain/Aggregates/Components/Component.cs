using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Components;

/// <summary>
/// Defines a reusable UI component schema (GAP-22).
/// A Component is the template (schema + zone assignment).
/// ComponentItems are the concrete data instances of that component.
/// </summary>
public sealed class Component : AggregateRoot<ComponentId>
{
    public const int MaxNameLength = 200;
    public const int MaxZoneLength = 100;

    private readonly List<FieldDefinition> _fields = [];

    private Component() : base() { } // EF Core

    private Component(ComponentId id, TenantId tenantId, SiteId siteId, string name, string zone)
  : base(id)
    {
        TenantId = tenantId;
    SiteId = siteId;
    Name = name;
    Zone = zone;
     CreatedAt = DateTimeOffset.UtcNow;
     UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    /// <summary>The designer zone this component can be placed in, e.g. "hero", "sidebar".</summary>
    public string Zone { get; private set; } = string.Empty;
  public int UsageCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<FieldDefinition> Fields => _fields.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────────────────

 public static Component Create(TenantId tenantId, SiteId siteId, string name, string zone)
    {
       ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
    ArgumentException.ThrowIfNullOrWhiteSpace(zone, nameof(zone));
        if (name.Length > MaxNameLength)
  throw new DomainException($"Component name must not exceed {MaxNameLength} characters.");
        return new Component(ComponentId.New(), tenantId, siteId, name.Trim(), zone.Trim());
    }

    // ── Fields ────────────────────────────────────────────────────────────

    public FieldDefinition AddField(
     string handle, string label, FieldType fieldType,
        bool isRequired = false, string? description = null)
  {
        if (_fields.Exists(f => f.Handle.Equals(handle, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException(
       "Component.DuplicateFieldHandle",
        $"A field with handle '{handle}' already exists on component '{Name}'.");

        var field = FieldDefinition.Create(
ContentTypeId.Empty, handle, label, fieldType,
     isRequired, false, false, _fields.Count, description);
    _fields.Add(field);
  UpdatedAt = DateTimeOffset.UtcNow;
        return field;
    }

    public void IncrementUsage() { UsageCount++; UpdatedAt = DateTimeOffset.UtcNow; }
    public void DecrementUsage() { UsageCount = Math.Max(0, UsageCount - 1); UpdatedAt = DateTimeOffset.UtcNow; }
}

/// <summary>
/// A concrete data instance of a <see cref="Component"/> schema (GAP-22).
/// FieldsJson stores the instance values matching the Component's field definitions.
/// </summary>
public sealed class ComponentItem : Entity<ComponentItemId>
{
    private ComponentItem() { } // EF Core

    private ComponentItem(ComponentItemId id, ComponentId componentId, TenantId tenantId, SiteId siteId)
  : base(id)
    {
    ComponentId = componentId;
  TenantId = tenantId;
     SiteId = siteId;
  CreatedAt = DateTimeOffset.UtcNow;
     UpdatedAt = DateTimeOffset.UtcNow;
    }

    public ComponentId ComponentId { get; private set; }
    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public string FieldsJson { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static ComponentItem Create(
 ComponentId componentId, TenantId tenantId, SiteId siteId, string fieldsJson = "{}")
    {
     ArgumentException.ThrowIfNullOrWhiteSpace(fieldsJson, nameof(fieldsJson));
     return new ComponentItem(ComponentItemId.New(), componentId, tenantId, siteId)
    { FieldsJson = fieldsJson };
  }

    public void UpdateFields(string fieldsJson)
    {
   ArgumentException.ThrowIfNullOrWhiteSpace(fieldsJson, nameof(fieldsJson));
  FieldsJson = fieldsJson;
  UpdatedAt = DateTimeOffset.UtcNow;
    }
}
