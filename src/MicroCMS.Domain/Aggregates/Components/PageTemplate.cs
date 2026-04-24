using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Components;

/// <summary>
/// Defines the zone layout and ordered component placements for a page (GAP-23).
/// One PageTemplate maps 1:1 with a <see cref="Pages.Page"/>.
/// </summary>
public sealed class PageTemplate : AggregateRoot<PageTemplateId>
{
    private readonly List<ComponentPlacement> _placements = [];

    private PageTemplate() : base() { } // EF Core

    private PageTemplate(PageTemplateId id, TenantId tenantId, PageId pageId) : base(id)
    {
  TenantId = tenantId;
     PageId = pageId;
    UpdatedAt = DateTimeOffset.UtcNow;
    }

 public TenantId TenantId { get; private set; }
    public PageId PageId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<ComponentPlacement> Placements => _placements.AsReadOnly();

    public static PageTemplate Create(TenantId tenantId, PageId pageId) =>
    new(PageTemplateId.New(), tenantId, pageId);

    public ComponentPlacement AddPlacement(ComponentId componentId, string zone, int sortOrder)
    {
    ArgumentException.ThrowIfNullOrWhiteSpace(zone, nameof(zone));
        var placement = new ComponentPlacement(Guid.NewGuid(), componentId, zone, sortOrder);
 _placements.Add(placement);
    _placements.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
     UpdatedAt = DateTimeOffset.UtcNow;
     return placement;
    }

    public void RemovePlacement(Guid placementId)
    {
    var existing = _placements.FirstOrDefault(p => p.Id == placementId)
  ?? throw new DomainException($"Placement '{placementId}' not found.");
     _placements.Remove(existing);
   UpdatedAt = DateTimeOffset.UtcNow;
  }

    public void ReorderPlacement(Guid placementId, int newSortOrder)
    {
    var p = _placements.FirstOrDefault(x => x.Id == placementId)
    ?? throw new DomainException($"Placement '{placementId}' not found.");
  _placements.Remove(p);
  _placements.Add(p.WithSortOrder(newSortOrder));
    _placements.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
       UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>An ordered component instance placed in a named zone on a page template.</summary>
public sealed class ComponentPlacement
{
  private ComponentPlacement() { } // EF Core

    internal ComponentPlacement(Guid id, ComponentId componentId, string zone, int sortOrder)
    {
        Id = id;
        ComponentId = componentId;
     Zone = zone;
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }
    public ComponentId ComponentId { get; private set; }
    public string Zone { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    internal ComponentPlacement WithSortOrder(int newSortOrder) =>
        new(Id, ComponentId, Zone, newSortOrder);
}
