namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public class SiteDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Handle { get; init; } = string.Empty;
    public string DefaultLocale { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string? CustomDomain { get; init; }
}

public sealed class SiteDetailDto : SiteDto
{
    public string TenantId { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
}

public sealed class SiteSettingsDto
{
    public string SiteId { get; init; } = string.Empty;
    public string? PreviewUrlTemplate { get; init; }
    public bool VersioningEnabled { get; init; }
    public bool WorkflowEnabled { get; init; }
    public bool SchedulingEnabled { get; init; }
    public bool PreviewEnabled { get; init; }
    public bool AiEnabled { get; init; }
    public List<string> CorsOrigins { get; init; } = [];
    public List<string> Locales { get; init; } = [];
}

public sealed class UpdateSiteSettingsRequest
{
    public string? PreviewUrlTemplate { get; set; }
    public bool VersioningEnabled { get; set; }
    public bool WorkflowEnabled { get; set; }
    public bool SchedulingEnabled { get; set; }
    public bool PreviewEnabled { get; set; }
    public bool AiEnabled { get; set; }
    public List<string> CorsOrigins { get; set; } = [];
    public List<string> Locales { get; set; } = [];
}
