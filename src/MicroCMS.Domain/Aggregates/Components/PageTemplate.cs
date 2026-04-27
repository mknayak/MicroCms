using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Components;

/// <summary>
/// Defines the zone layout and ordered component placements for a page (GAP-23).
/// One PageTemplate maps 1:1 with a Page.
/// Placements are stored as a nested JSON tree to support grid-row containers.
/// </summary>
public sealed class PageTemplate : AggregateRoot<PageTemplateId>
{
    private readonly List<ComponentPlacement> _placements = [];

    private PageTemplate() : base() { }

    private PageTemplate(PageTemplateId id, TenantId tenantId, PageId pageId) : base(id)
    {
TenantId = tenantId;
     PageId = pageId;
        PlacementsJson = "[]";
UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
  public PageId PageId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Serialised <c>PlacementNode[]</c> tree (component leaves and grid-row branches).
    /// Use this for save/load. <see cref="Placements"/> is kept for backwards compat.
    /// </summary>
    public string PlacementsJson { get; private set; } = "[]";

    // Kept for EF navigation / legacy queries
    public IReadOnlyList<ComponentPlacement> Placements => _placements.AsReadOnly();

    public static PageTemplate Create(TenantId tenantId, PageId pageId) =>
        new(PageTemplateId.New(), tenantId, pageId);

    /// <summary>Replaces the full placement tree from serialised JSON.</summary>
    public void SavePlacements(string placementsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placementsJson, nameof(placementsJson));
        PlacementsJson = placementsJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Legacy flat API (kept for compatibility) ──────────────────────────

    public ComponentPlacement AddPlacement(ComponentId componentId, string zone, int sortOrder,
      ComponentItemId? boundItemId = null, bool isLayoutDefault = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zone, nameof(zone));
        var placement = new ComponentPlacement(Guid.NewGuid(), componentId, zone, sortOrder,
   boundItemId, isLayoutDefault);
        _placements.Add(placement);
        _placements.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        UpdatedAt = DateTimeOffset.UtcNow;
   return placement;
    }

    public void RemovePlacement(Guid placementId)
    {
        var existing = _placements.FirstOrDefault(p => p.Id == placementId)
            ?? throw new DomainException($"Placement '{placementId}' not found.");
        if (existing.IsLayoutDefault)
    throw new BusinessRuleViolationException(
            "PageTemplate.CannotRemoveLayoutDefault",
          "Layout-default placements cannot be removed from a page template.");
        _placements.Remove(existing);
UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void BindItem(Guid placementId, ComponentItemId? boundItemId)
    {
        var p = _placements.FirstOrDefault(x => x.Id == placementId)
   ?? throw new DomainException($"Placement '{placementId}' not found.");
p.SetBoundItem(boundItemId);
  UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>An ordered component instance placed in a named zone on a page template.</summary>
public sealed class ComponentPlacement
{
    private ComponentPlacement() { }

    internal ComponentPlacement(Guid id, ComponentId componentId, string zone, int sortOrder,
        ComponentItemId? boundItemId = null, bool isLayoutDefault = false)
    {
        Id = id;
        ComponentId = componentId;
        Zone = zone;
        SortOrder = sortOrder;
        BoundItemId = boundItemId;
        IsLayoutDefault = isLayoutDefault;
    }

    public Guid Id { get; private set; }
    public ComponentId ComponentId { get; private set; }
    public string Zone { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    /// <summary>The specific ComponentItem whose data this placement renders.</summary>
    public ComponentItemId? BoundItemId { get; private set; }

    /// <summary>When true this placement was inherited from the layout and cannot be removed.</summary>
    public bool IsLayoutDefault { get; private set; }

    internal void SetBoundItem(ComponentItemId? itemId) { BoundItemId = itemId; }

    internal ComponentPlacement WithSortOrder(int newSortOrder) =>
        new(Id, ComponentId, Zone, newSortOrder, BoundItemId, IsLayoutDefault);
}
