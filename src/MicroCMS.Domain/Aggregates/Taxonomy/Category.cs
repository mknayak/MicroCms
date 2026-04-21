using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Taxonomy;

/// <summary>
/// Category aggregate root. Supports hierarchical trees via optional <see cref="ParentId"/>.
/// Categories are site-scoped and can be assigned to entries of any content type.
/// </summary>
public sealed class Category : AggregateRoot
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 500;

    private Category() { } // EF Core

    private Category(
        CategoryId id,
        TenantId tenantId,
        SiteId siteId,
        string name,
        Slug slug,
        CategoryId? parentId,
        string? description)
    {
        Id = id;
        TenantId = tenantId;
        SiteId = siteId;
        Name = name;
        Slug = slug;
        ParentId = parentId;
        Description = description;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public CategoryId Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public CategoryId? ParentId { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsRoot => ParentId is null;

    public static Category Create(
        TenantId tenantId,
        SiteId siteId,
        string name,
        Slug slug,
        CategoryId? parentId = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Category name must not exceed {MaxNameLength} characters.");
        }

        return new Category(CategoryId.New(), tenantId, siteId, name.Trim(), slug, parentId, description?.Trim());
    }

    public void Rename(string name, Slug slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Category name must not exceed {MaxNameLength} characters.");
        }

        Name = name.Trim();
        Slug = slug;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MoveUnder(CategoryId? newParentId)
    {
        if (newParentId?.Value == Id.Value)
        {
            throw new BusinessRuleViolationException(
                "Category.CircularReference",
                "A category cannot be its own parent.");
        }

        ParentId = newParentId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
