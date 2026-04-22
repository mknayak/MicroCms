using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.ValueObjects;
using Riok.Mapperly.Abstractions;

namespace MicroCMS.Application.Features.Entries.Mappers;

/// <summary>
/// Source-generated mapper for Entry → DTO conversions.
/// Mapperly generates the implementation at compile time, eliminating reflection overhead.
/// </summary>
[Mapper]
public static partial class EntryMapper
{
    /// <summary>Maps an <see cref="Entry"/> aggregate to its full DTO representation.</summary>
    public static EntryDto ToDto(Entry entry) => new(
        entry.Id.Value,
        entry.TenantId.Value,
        entry.SiteId.Value,
        entry.ContentTypeId.Value,
        entry.Slug.Value,
        entry.Locale.Value,
        entry.AuthorId,
        entry.Status.ToString(),
        entry.CurrentVersionNumber,
        entry.FieldsJson,
        entry.CreatedAt,
        entry.UpdatedAt,
        entry.PublishedAt,
        entry.ScheduledPublishAt,
        entry.ScheduledUnpublishAt,
        entry.FolderId?.Value,
        entry.Seo is { } seo
            ? new SeoMetadataDto(seo.MetaTitle, seo.MetaDescription, seo.CanonicalUrl, seo.OgImage)
            : null);

    /// <summary>Maps an <see cref="Entry"/> aggregate to a lightweight list item DTO.</summary>
    [MapProperty(nameof(Entry.Id) + "." + nameof(Entry.Id.Value), nameof(EntryListItemDto.Id))]
    [MapProperty(nameof(Entry.SiteId) + "." + nameof(Entry.SiteId.Value), nameof(EntryListItemDto.SiteId))]
    [MapProperty(nameof(Entry.ContentTypeId) + "." + nameof(Entry.ContentTypeId.Value), nameof(EntryListItemDto.ContentTypeId))]
    [MapProperty(nameof(Entry.Slug) + "." + nameof(Entry.Slug.Value), nameof(EntryListItemDto.Slug))]
    [MapProperty(nameof(Entry.Locale) + "." + nameof(Entry.Locale.Value), nameof(EntryListItemDto.Locale))]
    public static partial EntryListItemDto ToListItemDto(Entry entry);

    /// <summary>Maps an <see cref="EntryVersion"/> entity to its DTO.</summary>
    [MapProperty(nameof(EntryVersion.Id), nameof(EntryVersionDto.Id))]
    [MapProperty(nameof(EntryVersion.EntryId) + "." + nameof(EntryVersion.EntryId.Value), nameof(EntryVersionDto.EntryId))]
    public static partial EntryVersionDto ToVersionDto(EntryVersion version);

    /// <summary>Projects a sequence of entries to list item DTOs.</summary>
    public static IReadOnlyList<EntryListItemDto> ToListItemDtos(IEnumerable<Entry> entries) =>
        entries.Select(ToListItemDto).ToList().AsReadOnly();

    /// <summary>Projects a sequence of versions to version DTOs.</summary>
    public static IReadOnlyList<EntryVersionDto> ToVersionDtos(IEnumerable<EntryVersion> versions) =>
        versions.Select(ToVersionDto).ToList().AsReadOnly();
}
