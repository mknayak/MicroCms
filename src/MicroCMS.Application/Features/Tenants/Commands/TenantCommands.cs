using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Tenants.Dtos;

namespace MicroCMS.Application.Features.Tenants.Commands;

[HasPolicy(ContentPolicies.TenantManage)]
public sealed record CreateTenantCommand(
    string Slug,
    string DisplayName,
  string DefaultLocale,
 string TimeZoneId = "UTC",
    bool AiEnabled = false) : ICommand<TenantDto>;

[HasPolicy(ContentPolicies.TenantManage)]
public sealed record UpdateTenantSettingsCommand(
    Guid TenantId,
    string DisplayName,
    string DefaultLocale,
    string TimeZoneId,
    bool AiEnabled,
    string? LogoUrl) : ICommand<TenantDto>;

[HasPolicy(ContentPolicies.TenantManage)]
public sealed record AddSiteCommand(
    Guid TenantId,
    string Name,
    string Handle,
    string DefaultLocale) : ICommand<SiteDto>;
