namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public sealed class PageTreeNode
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string PageType { get; init; } = string.Empty;
    public string? ParentId { get; init; }
    public int Depth { get; init; }
    public string? LayoutId { get; init; }
    public List<PageTreeNode> Children { get; init; } = [];
}

public sealed class PageDto
{
    public string Id { get; init; } = string.Empty;
    public string SiteId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string PageType { get; init; } = string.Empty;
    public string? ParentId { get; init; }
    public string? LayoutId { get; init; }
    public int Depth { get; init; }
}

public sealed class CreateStaticPageRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? LayoutId { get; set; }
}
