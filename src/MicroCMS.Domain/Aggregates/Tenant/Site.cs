using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Tenant;

/// <summary>
/// A site is a named, scoped partition of content within a tenant
/// (e.g. "www", "blog", "docs"). Sites share users but isolate content.
/// </summary>
public sealed class Site : Entity<SiteId>
{
    public const int MaxNameLength = 100;

    private Site() { } // EF Core

    private Site(SiteId id, TenantId tenantId, string name, Slug handle, Locale defaultLocale)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Handle = handle;
        DefaultLocale = defaultLocale;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Slug Handle { get; private set; } = null!;
    public Locale DefaultLocale { get; private set; } = null!;
    public CustomDomain? CustomDomain { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    internal static Site Create(
        TenantId tenantId,
        string name,
        Slug handle,
        Locale defaultLocale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Site name must not exceed {MaxNameLength} characters.");
        }

        return new Site(SiteId.New(), tenantId, name.Trim(), handle, defaultLocale);
    }

    internal void AssignCustomDomain(CustomDomain domain) =>
        CustomDomain = domain;

    internal void RemoveCustomDomain() =>
        CustomDomain = null;

    internal void Deactivate()
    {
        if (!IsActive)
        {
            throw new BusinessRuleViolationException(
                "Site.AlreadyInactive",
                $"Site '{Handle.Value}' is already inactive.");
        }

        IsActive = false;
    }

    internal void Activate()
    {
        if (IsActive)
        {
            throw new BusinessRuleViolationException(
                "Site.AlreadyActive",
                $"Site '{Handle.Value}' is already active.");
        }

        IsActive = true;
    }
}
