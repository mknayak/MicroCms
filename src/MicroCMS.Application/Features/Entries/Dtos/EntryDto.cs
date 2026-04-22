namespace MicroCMS.Application.Features.Entries.Dtos;

/// <summary>Full representation of an <see cref="MicroCMS.Domain.Aggregates.Content.Entry"/>.</summary>
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
    DateTimeOffset? ScheduledUnpublishAt,
    Guid? FolderId,
    SeoMetadataDto? Seo);

/// <summary>SEO metadata sub-DTO used within <see cref="EntryDto"/> (GAP-08).</summary>
public sealed record SeoMetadataDto(
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    string? OgImage);
