using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Pages;

/// <summary>
/// Represents a single node in the site's hierarchical page tree (GAP-21).
/// Static pages hold a linked entry; Collection pages define a content-type route pattern.
/// </summary>
public sealed class Page : AggregateRoot<PageId>
{
    public const int MaxTitleLength = 300;
    public const int MaxRoutePatternLength = 500;

    private Page() : base() { } // EF Core

    private Page(
      PageId id,
    TenantId tenantId,
        SiteId siteId,
        string title,
     Slug slug,
        PageType pageType,
        PageId? parentId) : base(id)
    {
        TenantId = tenantId;
        SiteId = siteId;
        Title = title;
        Slug = slug;
        PageType = pageType;
        ParentId = parentId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public PageType PageType { get; private set; }
    public PageId? ParentId { get; private set; }
    /// <summary>For Static pages: the linked entry that provides the page content.</summary>
    public EntryId? LinkedEntryId { get; private set; }
    /// <summary>For Collection pages: the content type driving the route.</summary>
    public ContentTypeId? CollectionContentTypeId { get; private set; }
    /// <summary>Route pattern, e.g. "/products/{slug}" (GAP-21).</summary>
    public string? RoutePattern { get; private set; }
    public int Depth { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────

    public static Page CreateStatic(
TenantId tenantId, SiteId siteId,
   string title, Slug slug,
        PageId? parentId = null, EntryId? linkedEntryId = null, int depth = 0)
    {
        Validate(title);
        var page = new Page(PageId.New(), tenantId, siteId, title, slug, PageType.Static, parentId)
        {
            LinkedEntryId = linkedEntryId,
            Depth = depth
        };
        return page;
    }

    public static Page CreateCollection(
       TenantId tenantId, SiteId siteId,
   string title, Slug slug,
        ContentTypeId contentTypeId, string routePattern,
   PageId? parentId = null, int depth = 0)
    {
        Validate(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePattern, nameof(routePattern));

        return new Page(PageId.New(), tenantId, siteId, title, slug, PageType.Collection, parentId)
        {
            CollectionContentTypeId = contentTypeId,
            RoutePattern = routePattern.Trim(),
            Depth = depth
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────

    public void MoveTo(PageId? newParentId, int newDepth)
    {
        if (newParentId == Id)
            throw new BusinessRuleViolationException("Page.CircularReference", "A page cannot be its own parent.");
        ParentId = newParentId;
        Depth = newDepth;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        Validate(title);
        Title = title.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void LinkEntry(EntryId entryId)
    {
        if (PageType != PageType.Static)
            throw new BusinessRuleViolationException("Page.NotStatic", "Only Static pages can be linked to an entry.");
        LinkedEntryId = entryId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void Validate(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        if (title.Length > MaxTitleLength)
            throw new DomainException($"Page title must not exceed {MaxTitleLength} characters.");
    }
}
