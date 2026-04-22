using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Services;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Infrastructure.Tenancy;

/// <summary>
/// Provisions a new tenant atomically within the current unit of work:
///   1. Creates and persists the <see cref="Tenant"/> aggregate (Provisioning → Active).
///   2. Adds a default site.
///   3. Creates the admin <see cref="User"/> and assigns <c>TenantAdmin</c> role.
///
/// All three steps share the same <see cref="ApplicationDbContext"/> scope so the
/// caller's UnitOfWorkBehavior commits everything in a single transaction.
/// </summary>
internal sealed class TenantOnboardingService(
    IRepository<Tenant, TenantId> tenantRepo,
    IRepository<User, UserId> userRepo)
    : ITenantOnboardingService
{
public async Task<TenantOnboardingResult> OnboardAsync(
     TenantOnboardingRequest request,
        CancellationToken cancellationToken = default)
 {
        var locale = Locale.Create(request.DefaultLocale);

     var settings = TenantSettings.Create(
     request.DisplayName, locale,
       timeZoneId: request.TimeZoneId);

        var slug = TenantSlug.Create(request.Slug);
        var tenant = Tenant.Create(slug, settings);

   // Transition from Provisioning → Active as part of onboarding
        tenant.Activate();

var site = tenant.AddSite(
    request.DefaultSiteName,
      Slug.Create("main"),
   locale);

        await tenantRepo.AddAsync(tenant, cancellationToken);

        // Create admin user scoped to the new tenant
 var adminEmail = EmailAddress.Create(request.AdminEmail);
        var adminName = PersonName.Create(request.AdminDisplayName);
     var adminUser = User.Create(tenant.Id, adminEmail, adminName);
        adminUser.AssignRole(WorkflowRole.Publisher, "TenantAdmin");

        await userRepo.AddAsync(adminUser, cancellationToken);

        return new TenantOnboardingResult(
     tenant.Id.Value,
   site.Id.Value,
            adminUser.Id.Value);
    }
}
