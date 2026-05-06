using System.Text.Json;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Components.Commands;
using MicroCMS.Application.Features.Components.Dtos;
using MicroCMS.Application.Features.Components.Queries;
using MicroCMS.Application.Features.Components.Services;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Components;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Components.Handlers;

// ── Shared helper ─────────────────────────────────────────────────────────────

file static class ComponentHandlerHelpers
{
    internal static async Task<ContentType> LoadBackingTypeAsync(
        IRepository<ContentType, ContentTypeId> contentTypeRepo,
        Component comp,
        CancellationToken ct)
    {
        if (comp.BackingContentTypeId is null)
            throw new NotFoundException(nameof(ContentType), comp.Id.Value);

        return await contentTypeRepo.GetByIdAsync(comp.BackingContentTypeId.Value, ct)
            ?? throw new NotFoundException(nameof(ContentType), comp.BackingContentTypeId.Value.Value);
    }
}

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class ComponentMapper
{
    /// <summary>
    /// Maps a Component + its backing ContentType (single source of truth for fields) to a DTO.
    /// </summary>
    internal static ComponentDto ToDto(Component c, ContentType backingType) => new(
        c.Id.Value,
        c.TenantId.Value,
        c.SiteId.Value,
        c.Name,
        c.Key,
        c.Description,
        c.Category,
        c.UsageCount,
        c.ItemCount,
        c.TemplateType.ToString(),
        c.TemplateContent,
        c.ThumbnailDataUri,
        backingType.Fields.OrderBy(f => f.SortOrder).Select(f => new ComponentFieldDto(
            f.Id, f.Handle, f.Label, f.FieldType.ToString(),
            f.IsRequired, f.IsLocalized, f.IsUnique, f.IsIndexed, f.IsList, f.SortOrder, f.Description
        )).ToList(),
        c.CreatedAt,
        c.UpdatedAt);

    internal static ComponentListItemDto ToListItemDto(Component c, int fieldCount) => new(
        c.Id.Value,
        c.Name,
        c.Key,
        c.Description,
        c.Category,
        c.UsageCount,
        c.ItemCount,
        fieldCount,
        c.TemplateType.ToString(),
        c.ThumbnailDataUri,
        c.CreatedAt,
        c.UpdatedAt);

    internal static ComponentItemDto ToItemDto(ComponentItem ci, Component comp) => new(
        ci.Id.Value,
        ci.ComponentId.Value,
        comp.Name,
        comp.Key,
        ci.TenantId.Value,
        ci.SiteId.Value,
        ci.Title,
        ci.Status.ToString(),
        ParseJson(ci.FieldsJson),
        ci.UsedOnPages,
        ci.CreatedAt,
        ci.UpdatedAt);

    private static object ParseJson(string json)
    {
        try { return JsonSerializer.Deserialize<JsonElement>(json); }
        catch { return new { }; }
    }
}

// ── Command handlers ──────────────────────────────────────────────────────────

internal sealed class CreateComponentCommandHandler(
    IRepository<Component, ComponentId> repo,
    ICurrentUser currentUser,
    ComponentBackingTypeProvisioner backingTypeProvisioner)
    : IRequestHandler<CreateComponentCommand, Result<ComponentDto>>
{
    public async Task<Result<ComponentDto>> Handle(CreateComponentCommand request, CancellationToken cancellationToken)
    {
        var siteId = currentUser.SiteId;
        if (siteId is null)
            return Result.Failure<ComponentDto>(Error.Validation("Auth.NoSiteContext", "No site context in token. Call POST /auth/switch-site first."));

        var comp = Component.Create(
            currentUser.TenantId, siteId.Value,
            request.Name, request.Key, request.Description,
            request.Category);

        await repo.AddAsync(comp, cancellationToken);

        // Auto-create backing ContentType and seed initial fields from the request
        var backingType = await backingTypeProvisioner.ProvisionAsync(comp, request.Fields, cancellationToken);

        return Result.Success(ComponentMapper.ToDto(comp, backingType));
    }
}

internal sealed class UpdateComponentCommandHandler(
    IRepository<Component, ComponentId> repo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo)
    : IRequestHandler<UpdateComponentCommand, Result<ComponentDto>>
{
    public async Task<Result<ComponentDto>> Handle(UpdateComponentCommand request, CancellationToken cancellationToken)
    {
        var comp = await repo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);

        comp.Update(request.Name, request.Description, request.Category);
        repo.Update(comp);

        if (comp.BackingContentTypeId is null)
            throw new NotFoundException(nameof(ContentType), request.ComponentId);

        var backingType = await contentTypeRepo.GetByIdAsync(comp.BackingContentTypeId.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(ContentType), comp.BackingContentTypeId.Value.Value);

        ReplaceContentTypeFields(backingType, request.Fields);
        contentTypeRepo.Update(backingType);

        return Result.Success(ComponentMapper.ToDto(comp, backingType));
    }

    private static void ReplaceContentTypeFields(ContentType contentType, IReadOnlyList<ComponentFieldInput> fields)
    {
        // Remove fields no longer in the list
        var incoming = fields.Select(f => f.Handle).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var existing in contentType.Fields.ToList())
        {
            if (!incoming.Contains(existing.Handle))
                contentType.RemoveField(existing.Id);
        }

        // Add or update
        for (int i = 0; i < fields.Count; i++)
        {
            var f = fields[i];
            if (!Enum.TryParse<FieldType>(f.FieldType, true, out var ft))
                ft = FieldType.ShortText;

            var existing = contentType.Fields.FirstOrDefault(x =>
                x.Handle.Equals(f.Handle, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
                contentType.AddField(f.Handle, f.Label, ft, f.IsRequired, f.IsLocalized, f.IsUnique, f.Description, null, f.IsIndexed, f.IsList);
            else
                contentType.UpdateField(existing.Id, f.Label, ft, f.IsRequired, f.IsLocalized, f.IsIndexed, f.IsList, i, f.Description);
        }
    }
}

internal sealed class DeleteComponentCommandHandler(
    IRepository<Component, ComponentId> repo)
    : IRequestHandler<DeleteComponentCommand, Result>
{
    public async Task<Result> Handle(DeleteComponentCommand request, CancellationToken cancellationToken)
  {
     var comp = await repo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
    ?? throw new NotFoundException(nameof(Component), request.ComponentId);
  repo.Remove(comp);
      return Result.Success();
    }
}

internal sealed class UpdateComponentTemplateCommandHandler(
    IRepository<Component, ComponentId> repo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo)
    : IRequestHandler<UpdateComponentTemplateCommand, Result<ComponentDto>>
{
    public async Task<Result<ComponentDto>> Handle(UpdateComponentTemplateCommand request, CancellationToken cancellationToken)
    {
        var comp = await repo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);

        if (!Enum.TryParse<RenderingTemplateType>(request.TemplateType, ignoreCase: true, out var templateType))
            throw new MicroCMS.Application.Common.Exceptions.ValidationException(
                [new FluentValidation.Results.ValidationFailure(
                    "TemplateType", $"'{request.TemplateType}' is not a valid TemplateType.")]);

        comp.UpdateTemplate(templateType, request.TemplateContent);
        repo.Update(comp);

        var backingType = await ComponentHandlerHelpers.LoadBackingTypeAsync(contentTypeRepo, comp, cancellationToken);
        return Result.Success(ComponentMapper.ToDto(comp, backingType));
    }
}

internal sealed class UpdateComponentThumbnailCommandHandler(
    IRepository<Component, ComponentId> repo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo)
    : IRequestHandler<UpdateComponentThumbnailCommand, Result<ComponentDto>>
{
    public async Task<Result<ComponentDto>> Handle(UpdateComponentThumbnailCommand request, CancellationToken cancellationToken)
    {
        var comp = await repo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);

        comp.UpdateThumbnail(request.ThumbnailDataUri);
        repo.Update(comp);

        var backingType = await ComponentHandlerHelpers.LoadBackingTypeAsync(contentTypeRepo, comp, cancellationToken);
        return Result.Success(ComponentMapper.ToDto(comp, backingType));
    }
}

internal sealed class CreateComponentItemCommandHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<ComponentItem, ComponentItemId> itemRepo)
    : IRequestHandler<CreateComponentItemCommand, Result<ComponentItemDto>>
{
    public async Task<Result<ComponentItemDto>> Handle(CreateComponentItemCommand request, CancellationToken cancellationToken)
    {
        var comp = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
   ?? throw new NotFoundException(nameof(Component), request.ComponentId);

        var item = ComponentItem.Create(
            new ComponentId(request.ComponentId),
comp.TenantId,
            comp.SiteId,
      request.Title,
         request.FieldsJson);

        comp.IncrementItemCount();
        compRepo.Update(comp);
        await itemRepo.AddAsync(item, cancellationToken);
        return Result.Success(ComponentMapper.ToItemDto(item, comp));
    }
}

internal sealed class UpdateComponentItemCommandHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<ComponentItem, ComponentItemId> itemRepo)
    : IRequestHandler<UpdateComponentItemCommand, Result<ComponentItemDto>>
{
    public async Task<Result<ComponentItemDto>> Handle(UpdateComponentItemCommand request, CancellationToken cancellationToken)
  {
        var comp = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
        ?? throw new NotFoundException(nameof(Component), request.ComponentId);
        var item = await itemRepo.GetByIdAsync(new ComponentItemId(request.ItemId), cancellationToken)
          ?? throw new NotFoundException(nameof(ComponentItem), request.ItemId);

    item.UpdateFields(request.Title, request.FieldsJson);
        itemRepo.Update(item);
        return Result.Success(ComponentMapper.ToItemDto(item, comp));
    }
}

internal sealed class PublishComponentItemCommandHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<ComponentItem, ComponentItemId> itemRepo)
    : IRequestHandler<PublishComponentItemCommand, Result>
{
    public async Task<Result> Handle(PublishComponentItemCommand request, CancellationToken cancellationToken)
    {
        _ = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
  ?? throw new NotFoundException(nameof(Component), request.ComponentId);
        var item = await itemRepo.GetByIdAsync(new ComponentItemId(request.ItemId), cancellationToken)
            ?? throw new NotFoundException(nameof(ComponentItem), request.ItemId);

item.Publish();
 itemRepo.Update(item);
 return Result.Success();
    }
}

internal sealed class ArchiveComponentItemCommandHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<ComponentItem, ComponentItemId> itemRepo)
    : IRequestHandler<ArchiveComponentItemCommand, Result>
{
    public async Task<Result> Handle(ArchiveComponentItemCommand request, CancellationToken cancellationToken)
    {
        _ = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
  ?? throw new NotFoundException(nameof(Component), request.ComponentId);
  var item = await itemRepo.GetByIdAsync(new ComponentItemId(request.ItemId), cancellationToken)
 ?? throw new NotFoundException(nameof(ComponentItem), request.ItemId);

        item.Archive();
        itemRepo.Update(item);
        return Result.Success();
    }
}

internal sealed class DeleteComponentItemCommandHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<ComponentItem, ComponentItemId> itemRepo)
    : IRequestHandler<DeleteComponentItemCommand, Result>
{
    public async Task<Result> Handle(DeleteComponentItemCommand request, CancellationToken cancellationToken)
    {
   var comp = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);
        var item = await itemRepo.GetByIdAsync(new ComponentItemId(request.ItemId), cancellationToken)
            ?? throw new NotFoundException(nameof(ComponentItem), request.ItemId);

        comp.DecrementItemCount();
    compRepo.Update(comp);
        itemRepo.Remove(item);
    return Result.Success();
    }
}

// ── Query handlers ────────────────────────────────────────────────────────────

internal sealed class ListComponentsQueryHandler(
    IRepository<Component, ComponentId> repo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo,
    ICurrentUser currentUser)
    : IRequestHandler<ListComponentsQuery, Result<PagedList<ComponentListItemDto>>>
{
    public async Task<Result<PagedList<ComponentListItemDto>>> Handle(ListComponentsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<PagedList<ComponentListItemDto>>(Error.Validation("Auth.NoSiteContext", "No site context in token. Call POST /auth/switch-site first."));

        var items = await repo.ListAsync(new ComponentsBySiteSpec(siteId, request.Page, request.PageSize), cancellationToken);
        var total = await repo.CountAsync(new ComponentsBySiteCountSpec(siteId), cancellationToken);

        // Load backing content types to get accurate field counts
        var backingTypeIds = items
            .Where(c => c.BackingContentTypeId is not null)
            .Select(c => c.BackingContentTypeId!.Value)
            .Distinct()
            .ToList();

        var backingTypes = new Dictionary<ContentTypeId, ContentType>();
        foreach (var id in backingTypeIds)
        {
            var ct = await contentTypeRepo.GetByIdAsync(id, cancellationToken);
            if (ct is not null) backingTypes[id] = ct;
        }

        return Result.Success(PagedList<ComponentListItemDto>.Create(
            items.Select(c =>
            {
                var count = c.BackingContentTypeId is not null && backingTypes.TryGetValue(c.BackingContentTypeId.Value, out var bt)
                    ? bt.Fields.Count : 0;
                return ComponentMapper.ToListItemDto(c, count);
            }),
            request.Page, request.PageSize, total));
    }
}

internal sealed class GetComponentQueryHandler(
    IRepository<Component, ComponentId> repo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo)
    : IRequestHandler<GetComponentQuery, Result<ComponentDto>>
{
    public async Task<Result<ComponentDto>> Handle(GetComponentQuery request, CancellationToken cancellationToken)
    {
        var comp = await repo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);
        var backingType = await ComponentHandlerHelpers.LoadBackingTypeAsync(contentTypeRepo, comp, cancellationToken);
        return Result.Success(ComponentMapper.ToDto(comp, backingType));
    }
}

internal sealed class ListComponentItemsQueryHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<ComponentItem, ComponentItemId> itemRepo)
  : IRequestHandler<ListComponentItemsQuery, Result<PagedList<ComponentItemDto>>>
{
    public async Task<Result<PagedList<ComponentItemDto>>> Handle(ListComponentItemsQuery request, CancellationToken cancellationToken)
    {
        var compId = new ComponentId(request.ComponentId);
        var comp = await compRepo.GetByIdAsync(compId, cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);

    IReadOnlyList<ComponentItem> items;
   int total;

        if (!string.IsNullOrWhiteSpace(request.Status) &&
      Enum.TryParse<ComponentItemStatus>(request.Status, true, out var status))
 {
    items = await itemRepo.ListAsync(
     new ComponentItemsByComponentAndStatusSpec(compId, status, request.Page, request.PageSize),
   cancellationToken);
            total = await itemRepo.CountAsync(
      new ComponentItemsByComponentAndStatusSpec(compId, status, 1, int.MaxValue),
     cancellationToken);
        }
        else
        {
    items = await itemRepo.ListAsync(
       new ComponentItemsByComponentSpec(compId, request.Page, request.PageSize),
             cancellationToken);
       total = await itemRepo.CountAsync(new ComponentItemsCountSpec(compId), cancellationToken);
        }

        return Result.Success(PagedList<ComponentItemDto>.Create(
 items.Select(i => ComponentMapper.ToItemDto(i, comp)),
    request.Page, request.PageSize, total));
    }
}

internal sealed class GetComponentItemQueryHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<ComponentItem, ComponentItemId> itemRepo)
 : IRequestHandler<GetComponentItemQuery, Result<ComponentItemDto>>
{
    public async Task<Result<ComponentItemDto>> Handle(GetComponentItemQuery request, CancellationToken cancellationToken)
    {
        var comp = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);
        var item = await itemRepo.GetByIdAsync(new ComponentItemId(request.ItemId), cancellationToken)
      ?? throw new NotFoundException(nameof(ComponentItem), request.ItemId);
   return Result.Success(ComponentMapper.ToItemDto(item, comp));
    }
}
