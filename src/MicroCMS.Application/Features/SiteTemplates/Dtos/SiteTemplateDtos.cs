namespace MicroCMS.Application.Features.SiteTemplates.Dtos;

/// <summary>Full detail DTO returned for a single site template.</summary>
public sealed record SiteTemplateDto(
    Guid Id,
    Guid TenantId,
    Guid SiteId,
    Guid LayoutId,
    string? LayoutName,
    string Name,
    string? Description,
    string PlacementsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Summary row used in list responses.</summary>
public sealed record SiteTemplateListItemDto(
    Guid Id,
    string Name,
    string? Description,
    Guid LayoutId,
    string LayoutName,
    int PageCount,
    DateTimeOffset UpdatedAt);
