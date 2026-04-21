using FluentAssertions;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Events.Tenant;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.UnitTests.Fixtures;
using MicroCMS.Domain.ValueObjects;
using Xunit;

namespace MicroCMS.Domain.UnitTests.Aggregates;

public sealed class TenantAggregateTests
{
    // ── Creation ───────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInputs_ReturnsTenantInProvisioningStatus()
    {
        var tenant = Tenant.Create(DomainFixtures.ValidSlug, DomainFixtures.DefaultSettings);

        tenant.Status.Should().Be(TenantStatus.Provisioning);
        tenant.Sites.Should().BeEmpty();
    }

    [Fact]
    public void Create_RaisesTenantCreatedEvent()
    {
        var tenant = Tenant.Create(DomainFixtures.ValidSlug, DomainFixtures.DefaultSettings);

        tenant.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TenantCreatedEvent>();
    }

    // ── Activation / Suspension ────────────────────────────────────────────

    [Fact]
    public void Activate_FromProvisioning_SetsStatusToActive()
    {
        var tenant = Tenant.Create(DomainFixtures.ValidSlug, DomainFixtures.DefaultSettings);
        tenant.Activate();
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ThrowsBusinessRuleViolation()
    {
        var tenant = CreateActiveTenant();
        var act = () => tenant.Activate();
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*already active*");
    }

    [Fact]
    public void Suspend_WhenActive_SetsStatusToSuspended()
    {
        var tenant = CreateActiveTenant();
        tenant.Suspend("Outstanding invoice");
        tenant.Status.Should().Be(TenantStatus.Suspended);
    }

    [Fact]
    public void Suspend_WhenNotActive_ThrowsInvalidStateTransition()
    {
        var tenant = Tenant.Create(DomainFixtures.ValidSlug, DomainFixtures.DefaultSettings);
        var act = () => tenant.Suspend("reason");
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Suspend_WithEmptyReason_ThrowsArgumentException()
    {
        var tenant = CreateActiveTenant();
        var act = () => tenant.Suspend(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    // ── Sites ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddSite_WhenActive_SiteIsAdded()
    {
        var tenant = CreateActiveTenant();
        var site = tenant.AddSite("WWW", Slug.Create("www"), DomainFixtures.DefaultLocale);

        tenant.Sites.Should().ContainSingle();
        site.Handle.Value.Should().Be("www");
    }

    [Fact]
    public void AddSite_RaisesSiteCreatedEvent()
    {
        var tenant = CreateActiveTenant();
        tenant.ClearDomainEvents();
        tenant.AddSite("WWW", Slug.Create("www"), DomainFixtures.DefaultLocale);

        tenant.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SiteCreatedEvent>();
    }

    [Fact]
    public void AddSite_DuplicateHandle_ThrowsBusinessRuleViolation()
    {
        var tenant = CreateActiveTenant();
        tenant.AddSite("WWW", Slug.Create("www"), DomainFixtures.DefaultLocale);
        var act = () => tenant.AddSite("WWW Again", Slug.Create("www"), DomainFixtures.DefaultLocale);
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*already exists*");
    }

    [Fact]
    public void AddSite_WhenSuspended_ThrowsBusinessRuleViolation()
    {
        var tenant = CreateActiveTenant();
        tenant.Suspend("test");
        var act = () => tenant.AddSite("Blog", Slug.Create("blog"), DomainFixtures.DefaultLocale);
        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*Active*");
    }

    // ── Custom domain ─────────────────────────────────────────────────────

    [Fact]
    public void AssignCustomDomain_ToSite_Succeeds()
    {
        var tenant = CreateActiveTenant();
        var site = tenant.AddSite("WWW", Slug.Create("www"), DomainFixtures.DefaultLocale);
        var domain = CustomDomain.Create("www.acme.com");

        tenant.AssignCustomDomain(site.Id, domain);

        tenant.Sites[0].CustomDomain!.Value.Should().Be("www.acme.com");
    }

    [Fact]
    public void AssignCustomDomain_SameDomainTwoDifferentSites_ThrowsBusinessRuleViolation()
    {
        var tenant = CreateActiveTenant();
        var site1 = tenant.AddSite("WWW", Slug.Create("www"), DomainFixtures.DefaultLocale);
        var site2 = tenant.AddSite("Blog", Slug.Create("blog"), DomainFixtures.DefaultLocale);
        var domain = CustomDomain.Create("www.acme.com");

        tenant.AssignCustomDomain(site1.Id, domain);
        var act = () => tenant.AssignCustomDomain(site2.Id, domain);

        act.Should().Throw<BusinessRuleViolationException>().WithMessage("*already assigned*");
    }

    // ── Settings / Quota ──────────────────────────────────────────────────

    [Fact]
    public void UpdateSettings_ReplacesSettings()
    {
        var tenant = CreateActiveTenant();
        var newSettings = DomainFixtures.DefaultSettings.With(displayName: "New Name");
        tenant.UpdateSettings(newSettings);
        tenant.Settings.DisplayName.Should().Be("New Name");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Tenant CreateActiveTenant()
    {
        var tenant = Tenant.Create(DomainFixtures.ValidSlug, DomainFixtures.DefaultSettings);
        tenant.Activate();
        tenant.ClearDomainEvents();
        return tenant;
    }
}
