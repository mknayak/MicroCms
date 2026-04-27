namespace MicroCMS.Application.Features.Entries.Dtos;

/// <summary>
/// Lightweight projection used in paginated list responses.
/// Omits <c>Fields</c> to keep payload sizes small.
/// <para>
/// <c>Title</c> is extracted from the entry's <c>FieldsJson</c> by the query handler (looks for a
/// "title" key). <c>ContentTypeName</c> and <c>AuthorName</c> are populated via denormalised joins
/// in the query handler when a direct-projection query path is used; otherwise null.
/// </para>
/// </summary>
public sealed record EntryListItemDto(
    Guid Id,
    Guid SiteId,
    Guid ContentTypeId,
    string? ContentTypeName,
    string Slug,
    string? Title,
    string Locale,
    Guid AuthorId,
    string? AuthorName,
    string Status,
    int CurrentVersionNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ScheduledPublishAt);
