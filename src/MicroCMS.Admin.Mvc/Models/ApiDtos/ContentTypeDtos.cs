namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public sealed class ContentTypeListItem
{
    public string Id { get; init; } = string.Empty;
    public string Handle { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string LocalizationMode { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public int EntryCount { get; init; }
    public int FieldCount { get; init; }
    public string UpdatedAt { get; init; } = string.Empty;
}

public sealed class ContentTypeDto
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string SiteId { get; init; } = string.Empty;
    public string Handle { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string LocalizationMode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public List<FieldDefinitionDto> Fields { get; init; } = [];
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

public sealed class FieldDefinitionDto
{
    public string Id { get; init; } = string.Empty;
    public string Handle { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public bool IsLocalized { get; init; }
    public bool IsUnique { get; init; }
    public bool IsIndexed { get; init; }
    public bool IsList { get; init; }
    public int SortOrder { get; init; }
    public string? Description { get; init; }
    public string GroupName { get; init; } = "Default";
    public bool IsInherited { get; init; }
    public List<string>? Options { get; init; }
}

public sealed class CreateContentTypeRequest
{
    public string Handle { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string LocalizationMode { get; set; } = "Single";
    public string Kind { get; set; } = "Content";
}

public sealed class UpdateContentTypeRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LocalizationMode { get; set; }
}

public sealed class AddFieldRequest
{
    public string Handle { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = "ShortText";
    public bool IsRequired { get; set; }
    public bool IsLocalized { get; set; }
    public bool IsUnique { get; set; }
    public bool IsIndexed { get; set; }
    public bool IsList { get; set; }
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public string GroupName { get; set; } = "Default";
    public List<string>? Options { get; set; }
}

public sealed class UpdateFieldRequest
{
    public string Label { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsLocalized { get; set; }
    public bool IsUnique { get; set; }
    public bool IsIndexed { get; set; }
    public bool IsList { get; set; }
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public string GroupName { get; set; } = "Default";
    public List<string>? Options { get; set; }
}
