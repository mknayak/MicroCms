using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;

namespace MicroCMS.Application.Features.ContentTypes.Dtos;

public sealed record ContentTypeDto(
    Guid Id,
    Guid TenantId,
 Guid SiteId,
    string Handle,
    string DisplayName,
    string? Description,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<FieldDefinitionDto> Fields);

public sealed record FieldDefinitionDto(
    Guid Id,
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired,
    bool IsLocalized,
bool IsUnique,
    int SortOrder,
    string? Description);

public sealed record ContentTypeListItemDto(
    Guid Id,
    string Handle,
    string DisplayName,
    string Status,
 int FieldCount,
    DateTimeOffset UpdatedAt);
