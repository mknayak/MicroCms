using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.Delivery.Queries;

/// <summary>
/// Returns a single published entry by slug, site, and locale.
/// No [HasPolicy] — delivery endpoints are authenticated via API key, not JWT roles.
/// </summary>
public sealed record GetPublishedEntryBySlugQuery(
    Guid SiteId,
    string ContentTypeKey,
    string Slug,
    string Locale = "en") : IQuery<DeliveryEntryDto>;

/// <summary>Returns a paginated list of published entries for a content type.</summary>
public sealed record ListPublishedEntriesQuery(
    Guid SiteId,
    string ContentTypeKey,
    string? Locale = null,
    int Page = 1,
    int PageSize = 50) : IQuery<PagedList<DeliveryEntryDto>>;

/// <summary>Returns a published page by its slug path.</summary>
public sealed record GetPublishedPageBySlugQuery(
    Guid SiteId,
    string Slug) : IQuery<DeliveryPageDto>;

/// <summary>Returns the page tree for a site (shallow — no entry data).</summary>
public sealed record ListPublishedPagesQuery(
    Guid SiteId) : IQuery<IReadOnlyList<DeliveryPageDto>>;

/// <summary>Returns all published component items for a given component key.</summary>
public sealed record ListPublishedComponentItemsQuery(
    Guid SiteId,
    string ComponentKey,
    int Page = 1,
    int PageSize = 50) : IQuery<PagedList<DeliveryComponentItemDto>>;

/// <summary>Returns a single published component item by ID.</summary>
public sealed record GetPublishedComponentItemQuery(
    Guid SiteId,
    Guid ItemId) : IQuery<DeliveryComponentItemDto>;

/// <summary>Returns a paginated list of available media assets for a site.</summary>
public sealed record ListDeliveryMediaAssetsQuery(
    Guid SiteId,
    int Page = 1,
    int PageSize = 50) : IQuery<PagedList<DeliveryMediaAssetDto>>;

/// <summary>Returns a single available media asset by ID, with a resolved URL.</summary>
public sealed record GetDeliveryMediaAssetQuery(
    Guid SiteId,
    Guid AssetId) : IQuery<DeliveryMediaAssetDto>;

/// <summary>
/// Renders a full page server-side by walking:
///   Page → PageTemplate → ComponentPlacements → ComponentRenderer → Layout shell.
/// Returns either a complete HTML document or a dictionary of per-zone HTML fragments.
/// </summary>
public sealed record RenderPageBySlugQuery(
    Guid SiteId,
    string Slug,
    string Locale = "en") : IQuery<RenderedPageDto>;
