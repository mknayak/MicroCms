using HotChocolate;
using HotChocolate.Types;
using MediatR;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Application.Features.ContentTypes.Queries;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Queries.GetEntry;
using MicroCMS.Application.Features.Entries.Queries.ListEntries;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Application.Features.Media.Queries;
using MicroCMS.Application.Features.Search.Queries;
using MicroCMS.Application.Features.Taxonomy.Dtos;
using MicroCMS.Application.Features.Taxonomy.Queries;
using MicroCMS.GraphQL.DataLoaders;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;

namespace MicroCMS.GraphQL.Queries;

/// <summary>Root GraphQL query type — mirrors the REST read surface.</summary>
[GraphQLName("Query")]
public sealed class RootQuery
{
    // ── Entries ────────────────────────────────────────────────────────────

    /// <summary>Returns a single entry by its ID.</summary>
    [UseProjection]
    public async Task<EntryDto?> EntryAsync(
        Guid id,
 EntryByIdDataLoader dataLoader,
        CancellationToken cancellationToken)
        => await dataLoader.LoadAsync(id, cancellationToken);

    /// <summary>Returns a paginated list of entries for a site.</summary>
    public async Task<PagedList<EntryListItemDto>> EntriesAsync(
  Guid siteId,
        string? statusFilter,
        int page,
    int pageSize,
  [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
     var query = new ListEntriesQuery(siteId, statusFilter, page, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return result.IsSuccess
            ? result.Value
            : PagedList<EntryListItemDto>.Create(Array.Empty<EntryListItemDto>(), page, pageSize, 0);
    }

    // ── Content types ──────────────────────────────────────────────────────

    /// <summary>Returns a single content type by ID.</summary>
    public async Task<ContentTypeDto?> ContentTypeAsync(
        Guid id,
        [Service] IMediator mediator,
      CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetContentTypeQuery(id), cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>Returns a paginated list of content types for a site.</summary>
    public async Task<PagedList<ContentTypeListItemDto>> ContentTypesAsync(
        Guid? siteId,
        int page,
        int pageSize,
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListContentTypesQuery(siteId, page, pageSize), cancellationToken);
      return result.IsSuccess
            ? result.Value
            : PagedList<ContentTypeListItemDto>.Create(Array.Empty<ContentTypeListItemDto>(), page, pageSize, 0);
    }

    // ── Media ──────────────────────────────────────────────────────────────

    /// <summary>Returns a single media asset by ID.</summary>
    public async Task<MediaAssetDto?> MediaAssetAsync(
        Guid id,
        MediaAssetByIdDataLoader dataLoader,
  CancellationToken cancellationToken)
        => await dataLoader.LoadAsync(id, cancellationToken);

    /// <summary>Returns a paginated list of media assets for a site.</summary>
    public async Task<PagedList<MediaAssetListItemDto>> MediaAssetsAsync(
        Guid siteId,
        int page,
        int pageSize,
   [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListMediaAssetsQuery(siteId, page, pageSize), cancellationToken);
        return result.IsSuccess
            ? result.Value
 : PagedList<MediaAssetListItemDto>.Create(Array.Empty<MediaAssetListItemDto>(), page, pageSize, 0);
    }

    // ── Taxonomy ───────────────────────────────────────────────────────────

    /// <summary>Returns all categories for a site.</summary>
    public async Task<IReadOnlyList<CategoryDto>> CategoriesAsync(
     Guid siteId,
      [Service] IMediator mediator,
 CancellationToken cancellationToken)
    {
 var result = await mediator.Send(new ListCategoriesQuery(siteId), cancellationToken);
        return result.IsSuccess ? result.Value : Array.Empty<CategoryDto>();
    }

    /// <summary>Returns all tags for a site.</summary>
    public async Task<IReadOnlyList<TagDto>> TagsAsync(
        Guid siteId,
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListTagsQuery(siteId), cancellationToken);
        return result.IsSuccess ? result.Value : Array.Empty<TagDto>();
    }

    // ── Search ─────────────────────────────────────────────────────────────

    /// <summary>Full-text search over published entries.</summary>
  public async Task<SearchResults> SearchAsync(
      string query,
        Guid? siteId,
        Guid? contentTypeId,
    string? locale,
        string? status,
        int page,
        int pageSize,
    [Service] IMediator mediator,
    CancellationToken cancellationToken)
    {
   var searchQuery = new SearchEntriesQuery(
   query, siteId, contentTypeId, locale, status ?? "Published", page, pageSize);

        var result = await mediator.Send(searchQuery, cancellationToken);
   return result.IsSuccess
          ? result.Value
        : new SearchResults(Array.Empty<SearchHit>(), 0, page, pageSize);
    }
}
