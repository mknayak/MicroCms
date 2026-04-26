namespace MicroCMS.Application.Features.Delivery.Dtos;

/// <summary>A published entry returned by the Delivery API.</summary>
public sealed record DeliveryEntryDto(
    Guid Id,
    Guid SiteId,
    Guid ContentTypeId,
    string ContentTypeKey,
 string Slug,
    string Locale,
    string Status,
    object Fields,
    SeoDto? Seo,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAt);

/// <summary>SEO metadata for a delivered entry or page.</summary>
public sealed record SeoDto(
    string? Title,
    string? Description,
    string? OgImage,
    string? CanonicalUrl);

/// <summary>A published page node returned by the Delivery API.</summary>
public sealed record DeliveryPageDto(
    Guid Id,
    Guid SiteId,
    string Title,
 string Slug,
    string PageType,
    Guid? ParentId,
    Guid? LinkedEntryId,
    Guid? CollectionContentTypeId,
    string? RoutePattern,
    int Depth);

/// <summary>A published component item returned by the Delivery API.</summary>
public sealed record DeliveryComponentItemDto(
    Guid Id,
    Guid ComponentId,
    string ComponentKey,
    string Title,
    object Fields);

/// <summary>A media asset returned by the Delivery API, including a resolved URL.</summary>
public sealed record DeliveryMediaAssetDto(
    Guid Id,
    Guid SiteId,
    string FileName,
    string MimeType,
 long SizeBytes,
    int? WidthPx,
    int? HeightPx,
    string? AltText,
 IReadOnlyList<string> Tags,
    /// <summary>
    /// Ready-to-use URL. Public assets: direct provider URL.
    /// Private assets: time-limited signed URL (valid 1 hour).
    /// </summary>
    string Url,
    DateTimeOffset UpdatedAt);

/// <summary>
/// The result of a full server-side page render.
/// When <see cref="Html"/> is set the client receives a complete HTML document.
/// When <see cref="Zones"/> is set (Accept: application/json) the client receives
/// each zone's HTML independently so it can embed them in its own shell.
/// </summary>
public sealed record RenderedPageDto(
    Guid PageId,
    string Slug,
 string Title,
    /// <summary>Full HTML document. Non-null when rendered with a Layout shell.</summary>
    string? Html,
 /// <summary>Per-zone HTML fragments. Non-null when no Layout is configured (headless fallback).</summary>
    IReadOnlyDictionary<string, string>? Zones,
    SeoDto? Seo);
