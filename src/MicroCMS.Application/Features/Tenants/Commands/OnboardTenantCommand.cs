using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Services;

namespace MicroCMS.Application.Features.Tenants.Commands;

/// <summary>
/// Full tenant provisioning in a single atomic operation:
/// tenant + default site + admin user with TenantAdmin role.
/// Only callable by SystemAdmin.
/// </summary>
[HasPolicy(ContentPolicies.SystemAdmin)]
public sealed record OnboardTenantCommand(
    string Slug,
  string DisplayName,
    string DefaultLocale,
    string TimeZoneId,
    string AdminEmail,
    string AdminDisplayName,
    string DefaultSiteName = "Main") : ICommand<TenantOnboardingResult>;
