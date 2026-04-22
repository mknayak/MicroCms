using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Taxonomy;

/// <summary>
/// Tag aggregate root. Tags are flat (non-hierarchical) and site-scoped.
/// </summary>
public sealed class Tag : AggregateRoot<TagId>
{
    public const int MaxNameLength = 100;

    private Tag() : base() { } // EF Core

    private Tag(TagId id, TenantId tenantId, SiteId siteId, string name, Slug slug) : base(id)
    {
        TenantId = tenantId;
        SiteId = siteId;
        Name = name;
        Slug = slug;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public static Tag Create(TenantId tenantId, SiteId siteId, string name, Slug slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Tag name must not exceed {MaxNameLength} characters.");
        }

        return new Tag(TagId.New(), tenantId, siteId, name.Trim(), slug);
    }

    public void Rename(string name, Slug slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Tag name must not exceed {MaxNameLength} characters.");
        }

        Name = name.Trim();
        Slug = slug;
    }
}
