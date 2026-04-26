namespace MicroCMS.Application.Features.Layouts.Dtos;

public sealed record LayoutDto(
    Guid Id,
    Guid SiteId,
    string Name,
    string Key,
    string TemplateType,
    string? ShellTemplate,
    bool IsDefault,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record LayoutListItemDto(
    Guid Id,
    string Name,
    string Key,
    string TemplateType,
    bool IsDefault,
    DateTimeOffset UpdatedAt);
