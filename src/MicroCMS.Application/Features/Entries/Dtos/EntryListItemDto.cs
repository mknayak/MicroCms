namespace MicroCMS.Application.Features.Entries.Dtos;

/// <summary>
/// Lightweight projection used in paginated list responses.
/// Omits <c>FieldsJson</c> to keep payload sizes small.
/// </summary>
public sealed record EntryListItemDto(
    Guid Id,
    Guid SiteId,
    Guid ContentTypeId,
    string Slug,
    string Locale,
    string Status,
    int CurrentVersionNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);
