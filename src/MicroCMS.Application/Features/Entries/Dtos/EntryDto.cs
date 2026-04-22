namespace MicroCMS.Application.Features.Entries.Dtos;

/// <summary>
/// Full representation of an <see cref="MicroCMS.Domain.Aggregates.Content.Entry"/>,
/// returned by GetEntry and CreateEntry/UpdateEntry commands.
/// </summary>
public sealed record EntryDto(
    Guid Id,
    Guid TenantId,
    Guid SiteId,
    Guid ContentTypeId,
    string Slug,
    string Locale,
    Guid AuthorId,
    string Status,
    int CurrentVersionNumber,
    string FieldsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ScheduledPublishAt,
    DateTimeOffset? ScheduledUnpublishAt);
