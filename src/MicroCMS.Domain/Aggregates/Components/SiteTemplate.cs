using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Components;

/// <summary>
/// A reusable named template: defines a set of component placements for a specific Layout.
/// Many pages can be linked to the same SiteTemplate, inheriting its placements.
/// A page may still add/override placements on top of what the template defines.
/// </summary>
public sealed class SiteTemplate : AggregateRoot<SiteTemplateId>
{
    private SiteTemplate() : base() { }

    private SiteTemplate(SiteTemplateId id, TenantId tenantId, SiteId siteId,
        LayoutId layoutId, string name, string? description) : base(id)
    {
        TenantId = tenantId;
    SiteId = siteId;
        LayoutId = layoutId;
        Name = name;
        Description = description;
        PlacementsJson = "[]";
     CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
  }

    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }

    /// <summary>The layout this template is based on — defines the zones.</summary>
    public LayoutId LayoutId { get; private set; }

    public string Name { get; private set; } = string.Empty;
  public string? Description { get; private set; }

    /// <summary>
 /// Serialised PlacementNode[] tree. Same schema as PageTemplate.PlacementsJson.
    /// These placements are inherited by every page that uses this template.
    /// </summary>
    public string PlacementsJson { get; private set; } = "[]";

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────

    public static SiteTemplate Create(TenantId tenantId, SiteId siteId,
        LayoutId layoutId, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
return new SiteTemplate(SiteTemplateId.New(), tenantId, siteId, layoutId, name, description);
    }

    // ── Mutations ──────────────────────────────────────────────────────────

    public void Update(string name, string? description, LayoutId layoutId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
     Description = description?.Trim();
        LayoutId = layoutId;
     UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Replaces the full placement tree from serialised JSON.</summary>
    public void SavePlacements(string placementsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placementsJson, nameof(placementsJson));
        PlacementsJson = placementsJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
