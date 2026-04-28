using System.Text.Json;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Application.Features.Delivery.Queries;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Services;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.Specifications.Delivery;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Delivery.Handlers;

// ── Helpers ───────────────────────────────────────────────────────────────────

internal static class DeliveryMapper
{
    internal static DeliveryEntryDto ToDto(Entry e, string contentTypeKey) => new(
        e.Id.Value,
        e.SiteId.Value,
        e.ContentTypeId.Value,
        contentTypeKey,
        e.Slug.Value,
        e.Locale.Value,
        e.Status.ToString(),
        ParseJson(e.FieldsJson),
        e.PublishedAt,
        e.UpdatedAt);

    internal static DeliveryPageDto ToPageDto(Page p) => new(
      p.Id.Value,
        p.SiteId.Value,
        p.Title,
        p.Slug.Value,
        p.PageType.ToString(),
        p.ParentId?.Value,
        p.LinkedEntryId?.Value,
      p.CollectionContentTypeId?.Value,
        p.RoutePattern,
        p.Depth);

    internal static DeliveryComponentItemDto ToItemDto(ComponentItem ci, string componentKey) => new(
        ci.Id.Value,
        ci.ComponentId.Value,
        componentKey,
     ci.Title,
    ParseJson(ci.FieldsJson));

    private static object ParseJson(string json)
    {
        try { return JsonSerializer.Deserialize<JsonElement>(json); }
        catch { return new { }; }
    }
}

// ── Entry handlers ────────────────────────────────────────────────────────────

internal sealed class GetPublishedEntryBySlugQueryHandler(
    IRepository<ContentType, ContentTypeId> ctRepo,
    IRepository<Entry, EntryId> entryRepo)
    : IRequestHandler<GetPublishedEntryBySlugQuery, Result<DeliveryEntryDto>>
{
    public async Task<Result<DeliveryEntryDto>> Handle(
    GetPublishedEntryBySlugQuery request,
          CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);

        // Resolve content type by key (Handle)
        var contentTypes = await ctRepo.ListAsync(new ContentTypesBySiteSpec(siteId), cancellationToken);
        var ct = contentTypes.FirstOrDefault(c =>
            c.Handle.Equals(request.ContentTypeKey, StringComparison.OrdinalIgnoreCase))
        ?? throw new NotFoundException("ContentType", request.ContentTypeKey);

        var spec = new PublishedEntryBySlugSpec(siteId, request.Slug, request.Locale);
        var entries = await entryRepo.ListAsync(spec, cancellationToken);
        var entry = entries.FirstOrDefault(e => e.ContentTypeId == ct.Id)
              ?? throw new NotFoundException("Entry", request.Slug);

        return Result.Success(DeliveryMapper.ToDto(entry, ct.Handle));
    }
}

internal sealed class ListPublishedEntriesQueryHandler(
    IRepository<ContentType, ContentTypeId> ctRepo,
    IRepository<Entry, EntryId> entryRepo)
    : IRequestHandler<ListPublishedEntriesQuery, Result<PagedList<DeliveryEntryDto>>>
{
    public async Task<Result<PagedList<DeliveryEntryDto>>> Handle(
        ListPublishedEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);

        var contentTypes = await ctRepo.ListAsync(new ContentTypesBySiteSpec(siteId), cancellationToken);
        var ct = contentTypes.FirstOrDefault(c =>
     c.Handle.Equals(request.ContentTypeKey, StringComparison.OrdinalIgnoreCase))
?? throw new NotFoundException("ContentType", request.ContentTypeKey);

        var spec = new PublishedEntriesByContentTypeSpec(siteId, ct.Id, request.Locale, request.Page, request.PageSize);
        var countSpec = new PublishedEntriesByContentTypeSpec(siteId, ct.Id, request.Locale);

        var items = await entryRepo.ListAsync(spec, cancellationToken);
        var total = await entryRepo.CountAsync(countSpec, cancellationToken);

        return Result.Success(PagedList<DeliveryEntryDto>.Create(
    items.Select(e => DeliveryMapper.ToDto(e, ct.Handle)),
          request.Page, request.PageSize, total));
    }
}

// ── Page handlers ─────────────────────────────────────────────────────────────

internal sealed class GetPublishedPageBySlugQueryHandler(
    IRepository<Page, PageId> pageRepo)
    : IRequestHandler<GetPublishedPageBySlugQuery, Result<DeliveryPageDto>>
{
    public async Task<Result<DeliveryPageDto>> Handle(
        GetPublishedPageBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var pages = await pageRepo.ListAsync(new PageBySlugSpec(siteId, request.Slug), cancellationToken);
        var page = pages.FirstOrDefault()
                 ?? throw new NotFoundException("Page", request.Slug);

        return Result.Success(DeliveryMapper.ToPageDto(page));
    }
}

internal sealed class ListPublishedPagesQueryHandler(
    IRepository<Page, PageId> pageRepo)
    : IRequestHandler<ListPublishedPagesQuery, Result<IReadOnlyList<DeliveryPageDto>>>
{
    public async Task<Result<IReadOnlyList<DeliveryPageDto>>> Handle(
     ListPublishedPagesQuery request,
        CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var pages = await pageRepo.ListAsync(new PagesBySiteSpec(siteId), cancellationToken);
        IReadOnlyList<DeliveryPageDto> result = pages.Select(DeliveryMapper.ToPageDto).ToList();
        return Result.Success(result);
    }
}

// ── Component item handlers ───────────────────────────────────────────────────

internal sealed class ListPublishedComponentItemsQueryHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<ComponentItem, ComponentItemId> itemRepo)
    : IRequestHandler<ListPublishedComponentItemsQuery, Result<PagedList<DeliveryComponentItemDto>>>
{
    public async Task<Result<PagedList<DeliveryComponentItemDto>>> Handle(
        ListPublishedComponentItemsQuery request,
   CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var components = await compRepo.ListAsync(
        new MicroCMS.Domain.Specifications.Components.ComponentsBySiteSpec(siteId, 1, int.MaxValue),
       cancellationToken);

        var comp = components.FirstOrDefault(c =>
c.Key.Equals(request.ComponentKey, StringComparison.OrdinalIgnoreCase))
  ?? throw new NotFoundException("Component", request.ComponentKey);

        var spec = new MicroCMS.Domain.Specifications.Components.ComponentItemsByComponentAndStatusSpec(
       comp.Id, ComponentItemStatus.Published, request.Page, request.PageSize);
        var countSpec = new MicroCMS.Domain.Specifications.Components.ComponentItemsByComponentAndStatusSpec(
 comp.Id, ComponentItemStatus.Published, 1, int.MaxValue);

        var items = await itemRepo.ListAsync(spec, cancellationToken);
        var total = await itemRepo.CountAsync(countSpec, cancellationToken);

        return Result.Success(PagedList<DeliveryComponentItemDto>.Create(
            items.Select(i => DeliveryMapper.ToItemDto(i, comp.Key)),
     request.Page, request.PageSize, total));
    }
}

internal sealed class GetPublishedComponentItemQueryHandler(
IRepository<Component, ComponentId> compRepo,
    IRepository<ComponentItem, ComponentItemId> itemRepo)
 : IRequestHandler<GetPublishedComponentItemQuery, Result<DeliveryComponentItemDto>>
{
    public async Task<Result<DeliveryComponentItemDto>> Handle(
        GetPublishedComponentItemQuery request,
CancellationToken cancellationToken)
    {
        var item = await itemRepo.GetByIdAsync(new ComponentItemId(request.ItemId), cancellationToken)
            ?? throw new NotFoundException("ComponentItem", request.ItemId);

        if (item.Status != ComponentItemStatus.Published)
            throw new NotFoundException("ComponentItem", request.ItemId);

        if (item.SiteId != new SiteId(request.SiteId))
            throw new NotFoundException("ComponentItem", request.ItemId);

        var comp = await compRepo.GetByIdAsync(item.ComponentId, cancellationToken)
  ?? throw new NotFoundException("Component", item.ComponentId.Value);

        return Result.Success(DeliveryMapper.ToItemDto(item, comp.Key));
    }
}

// ── Media handlers ────────────────────────────────────────────────────────────

internal sealed class ListDeliveryMediaAssetsQueryHandler(
    IRepository<MediaAsset, MediaAssetId> assetRepo,
    IStorageProvider storage,
    IStorageSigningService signing)
    : IRequestHandler<ListDeliveryMediaAssetsQuery, Result<PagedList<DeliveryMediaAssetDto>>>
{
    public async Task<Result<PagedList<DeliveryMediaAssetDto>>> Handle(
        ListDeliveryMediaAssetsQuery request,
     CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var spec = new AvailableMediaAssetsBySiteSpec(siteId, request.Page, request.PageSize);
        var countSpec = new AvailableMediaAssetsBySiteSpec(siteId);

        var items = await assetRepo.ListAsync(spec, cancellationToken);
        var total = await assetRepo.CountAsync(countSpec, cancellationToken);

        var dtos = new List<DeliveryMediaAssetDto>(items.Count);
        foreach (var asset in items)
            dtos.Add(await MediaDeliveryHelper.ToDeliveryDtoAsync(asset, storage, signing, cancellationToken));

        return Result.Success(PagedList<DeliveryMediaAssetDto>.Create(dtos, request.Page, request.PageSize, total));
    }
}

internal sealed class GetDeliveryMediaAssetQueryHandler(
    IRepository<MediaAsset, MediaAssetId> assetRepo,
    IStorageProvider storage,
    IStorageSigningService signing)
    : IRequestHandler<GetDeliveryMediaAssetQuery, Result<DeliveryMediaAssetDto>>
{
    public async Task<Result<DeliveryMediaAssetDto>> Handle(
    GetDeliveryMediaAssetQuery request,
      CancellationToken cancellationToken)
    {
        var id = new MediaAssetId(request.AssetId);
        var siteId = new SiteId(request.SiteId);

        var results = await assetRepo.ListAsync(new AvailableMediaAssetByIdSpec(id), cancellationToken);
        var asset = results.FirstOrDefault(a => a.SiteId == siteId)
            ?? throw new NotFoundException(nameof(MediaAsset), request.AssetId);

        return Result.Success(await MediaDeliveryHelper.ToDeliveryDtoAsync(asset, storage, signing, cancellationToken));
    }
}

// ── Shared media URL helper ───────────────────────────────────────────────────

file static class MediaDeliveryHelper
{
    internal static async Task<DeliveryMediaAssetDto> ToDeliveryDtoAsync(
        MediaAsset asset,
        IStorageProvider storage,
        IStorageSigningService signing,
        CancellationToken cancellationToken)
    {
        string url;

        if (asset.Visibility == AssetVisibility.Private)
        {
            // Private: generate a 1-hour HMAC-signed URL.
            url = await signing.GenerateSignedUrlAsync(
                 asset.StorageKey,
                       TimeSpan.FromHours(1),
                      cancellationToken);
        }
        else
        {
            // Public: ask the storage provider for its direct URL.
            url = await storage.GetPublicUrlAsync(asset.StorageKey, cancellationToken);
        }

        return new DeliveryMediaAssetDto(
        asset.Id.Value,
   asset.SiteId.Value,
asset.Metadata.FileName,
  asset.Metadata.MimeType,
            asset.Metadata.SizeBytes,
     asset.Metadata.WidthPx,
    asset.Metadata.HeightPx,
        asset.AltText ?? asset.AiAltText,
    asset.Tags,
       url,
            asset.UpdatedAt);
    }
}
