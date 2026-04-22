using MicroCMS.Application.Features.Tenants.Dtos;
using MicroCMS.Domain.Aggregates.Tenant;

namespace MicroCMS.Application.Features.Tenants.Mappers;

/// <summary>
/// Manual mapper for Tenant → DTO conversions.
/// Manual is preferred here because Tenant owns Sites as a nested list
/// and Mapperly's path notation becomes verbose for this shape.
/// </summary>
public static class TenantMapper
{
    public static TenantDto ToDto(Tenant tenant) => new(
        tenant.Id.Value,
        tenant.Slug.Value,
        tenant.Settings.DisplayName,
        tenant.Settings.DefaultLocale.Value,
        tenant.Settings.TimeZoneId,
     tenant.Settings.AiEnabled,
        tenant.Settings.LogoUrl,
        tenant.Status.ToString(),
        tenant.CreatedAt,
        tenant.UpdatedAt,
        tenant.Sites.Select(ToSiteDto).ToList().AsReadOnly());

 public static TenantListItemDto ToListItemDto(Tenant tenant) => new(
        tenant.Id.Value,
     tenant.Slug.Value,
        tenant.Settings.DisplayName,
        tenant.Status.ToString(),
        tenant.CreatedAt);

    public static SiteDto ToSiteDto(Site site) => new(
   site.Id.Value,
        site.Name,
        site.Handle.Value,
        site.DefaultLocale.Value,
        site.IsActive,
        site.CustomDomain?.Value);
}
