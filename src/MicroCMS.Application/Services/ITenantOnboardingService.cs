using MicroCMS.Application.Features.Tenants.Dtos;

namespace MicroCMS.Application.Services;

/// <summary>
/// Orchestrates the full tenant provisioning flow:
/// 1. Creates the <c>Tenant</c> aggregate and persists it.
/// 2. Adds a default site.
/// 3. Creates the first admin <c>User</c> and assigns the TenantAdmin role.
///
/// Called from the <see cref="MicroCMS.Application.Features.Tenants.Commands.OnboardTenantCommand"/> handler.
/// All steps execute within the same unit of work so provisioning is atomic.
/// </summary>
public interface ITenantOnboardingService
{
    Task<TenantOnboardingResult> OnboardAsync(
        TenantOnboardingRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record TenantOnboardingRequest(
    string Slug,
  string DisplayName,
    string DefaultLocale,
    string TimeZoneId,
    string AdminEmail,
    string AdminDisplayName,
    string DefaultSiteName = "Main");

public sealed record TenantOnboardingResult(
    Guid TenantId,
    Guid DefaultSiteId,
    Guid AdminUserId);
