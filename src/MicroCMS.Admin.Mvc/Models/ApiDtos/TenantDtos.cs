namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public sealed class TenantListItem
{
    public string Id { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
}

public sealed class TenantDetail
{
    public string Id { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string DefaultLocale { get; init; } = string.Empty;
    public string TimeZoneId { get; init; } = string.Empty;
    public bool AiEnabled { get; init; }
    public string? LogoUrl { get; init; }
    public string Status { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public List<SiteDto> Sites { get; init; } = [];
}

public sealed class OnboardTenantRequest
{
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DefaultLocale { get; set; } = "en";
    public string TimeZoneId { get; set; } = "UTC";
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminDisplayName { get; set; } = string.Empty;
    public string? DefaultSiteName { get; set; }
}

public sealed class UpdateTenantSettingsRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string DefaultLocale { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public bool AiEnabled { get; set; }
    public string? LogoUrl { get; set; }
}

public sealed class CreateSiteRequest
{
    public string Name { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public string DefaultLocale { get; set; } = "en";
    public string? CustomDomain { get; set; }
}
