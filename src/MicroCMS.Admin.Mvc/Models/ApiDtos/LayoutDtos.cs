namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public class LayoutListItem
{
    public string Id { get; init; } = string.Empty;
    public string SiteId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string TemplateType { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

public sealed class LayoutDto : LayoutListItem
{
    public string TenantId { get; init; } = string.Empty;
    public string? ShellTemplate { get; init; }
    public bool IsShellCustomized { get; init; }
}

public sealed class CreateLayoutRequest
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string TemplateType { get; set; } = "Handlebars";
}

public sealed class UpdateLayoutRequest
{
    public string Name { get; set; } = string.Empty;
    public string TemplateType { get; set; } = string.Empty;
}
