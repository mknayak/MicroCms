namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public  class ComponentListItem
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Category { get; init; } = string.Empty;
    public int UsageCount { get; init; }
    public int ItemCount { get; init; }
    public int FieldCount { get; init; }
    public string TemplateType { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

public sealed class ComponentDto : ComponentListItem
{
    public string TenantId { get; init; } = string.Empty;
    public string SiteId { get; init; } = string.Empty;
    public string? TemplateContent { get; init; }
    public List<ComponentFieldDefinition> Fields { get; init; } = [];
}

public sealed class ComponentFieldDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Handle { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public bool IsLocalized { get; init; }
    public bool IsList { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
}

public sealed class ComponentItemDto
{
    public string Id { get; init; } = string.Empty;
    public string ComponentId { get; init; } = string.Empty;
    public string ComponentName { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Dictionary<string, object?> FieldsJson { get; init; } = [];
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

public sealed class CreateComponentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Content";
}
