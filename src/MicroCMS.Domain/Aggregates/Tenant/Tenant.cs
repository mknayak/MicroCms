using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Events.Tenant;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Tenant;

/// <summary>
/// Tenant aggregate root. A tenant is the top-level isolation boundary.
/// All content, users, settings, and quotas belong to a specific tenant.
/// </summary>
public sealed class Tenant : AggregateRoot
{
    private readonly List<Site> _sites = [];

    private Tenant() { } // EF Core

    private Tenant(
        TenantId id,
        TenantSlug slug,
        TenantSettings settings,
        TenantQuota quota)
    {
        Id = id;
        Slug = slug;
        Settings = settings;
        Quota = quota;
        Status = TenantStatus.Provisioning;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId Id { get; private set; }
    public TenantSlug Slug { get; private set; } = null!;
    public TenantSettings Settings { get; private set; } = null!;
    public TenantQuota Quota { get; private set; } = null!;
    public TenantStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<Site> Sites => _sites.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────────────────

    public static Tenant Create(
        TenantSlug slug,
        TenantSettings settings,
        TenantQuota? quota = null)
    {
        ArgumentNullException.ThrowIfNull(slug, nameof(slug));
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));

        var tenant = new Tenant(TenantId.New(), slug, settings, quota ?? TenantQuota.Default);
        tenant.RaiseDomainEvent(new TenantCreatedEvent(tenant.Id, slug.Value));
        return tenant;
    }

    // ── State transitions ──────────────────────────────────────────────────

    public void Activate()
    {
        if (Status == TenantStatus.Active)
        {
            throw new BusinessRuleViolationException(
                "Tenant.AlreadyActive", "Tenant is already active.");
        }

        Status = TenantStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new TenantActivatedEvent(Id));
    }

    public void Suspend(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        if (Status != TenantStatus.Active)
        {
            throw new InvalidStateTransitionException("Tenant", Status.ToString(), "Suspended");
        }

        Status = TenantStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new TenantSuspendedEvent(Id, reason));
    }

    public void MarkForDeletion()
    {
        if (Status == TenantStatus.PendingDeletion)
        {
            throw new BusinessRuleViolationException(
                "Tenant.AlreadyPendingDeletion", "Tenant is already pending deletion.");
        }

        Status = TenantStatus.PendingDeletion;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Settings ──────────────────────────────────────────────────────────

    public void UpdateSettings(TenantSettings newSettings)
    {
        ArgumentNullException.ThrowIfNull(newSettings, nameof(newSettings));
        Settings = newSettings;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateQuota(TenantQuota newQuota)
    {
        ArgumentNullException.ThrowIfNull(newQuota, nameof(newQuota));
        Quota = newQuota;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Sites ─────────────────────────────────────────────────────────────

    public Site AddSite(string name, Slug handle, Locale defaultLocale)
    {
        EnsureActive();

        if (_sites.Any(s => s.Handle.Value == handle.Value))
        {
            throw new BusinessRuleViolationException(
                "Tenant.DuplicateSiteHandle",
                $"A site with handle '{handle.Value}' already exists on this tenant.");
        }

        var site = Site.Create(Id, name, handle, defaultLocale);
        _sites.Add(site);
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new SiteCreatedEvent(Id, site.Id, handle.Value));
        return site;
    }

    public void DeactivateSite(SiteId siteId)
    {
        var site = GetSiteOrThrow(siteId);
        site.Deactivate();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AssignCustomDomain(SiteId siteId, CustomDomain domain)
    {
        EnsureActive();
        var site = GetSiteOrThrow(siteId);

        // Ensure no other site on this tenant uses the same domain
        if (_sites.Any(s => s.Id != siteId && s.CustomDomain?.Value == domain.Value))
        {
            throw new BusinessRuleViolationException(
                "Tenant.DomainAlreadyAssigned",
                $"Domain '{domain.Value}' is already assigned to another site.");
        }

        site.AssignCustomDomain(domain);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void EnsureActive()
    {
        if (Status != TenantStatus.Active)
        {
            throw new BusinessRuleViolationException(
                "Tenant.NotActive",
                $"Operation requires tenant to be Active. Current status: {Status}.");
        }
    }

    private Site GetSiteOrThrow(SiteId siteId) =>
        _sites.FirstOrDefault(s => s.Id == siteId)
        ?? throw new DomainException($"Site '{siteId}' not found on tenant '{Id}'.");
}
