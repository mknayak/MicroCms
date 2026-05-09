namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public sealed class CategoryDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ParentId { get; init; }
    public List<CategoryDto> Children { get; init; } = [];
    public int EntryCount { get; init; }
}

public sealed class TagDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int EntryCount { get; init; }
}

public sealed class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ParentId { get; set; }
}

public sealed class CreateTagRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
