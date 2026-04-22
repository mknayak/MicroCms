namespace MicroCMS.Application.Features.Taxonomy.Dtos;

public sealed record CategoryDto(
    Guid Id,
  Guid SiteId,
  string Name,
    string Slug,
 Guid? ParentId,
    string? Description,
    DateTimeOffset CreatedAt,
 DateTimeOffset UpdatedAt);

public sealed record TagDto(
 Guid Id,
    Guid SiteId,
    string Name,
 string Slug,
  DateTimeOffset CreatedAt);
