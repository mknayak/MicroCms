using System.Text.Json;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Components.Commands;
using MicroCMS.Application.Features.Components.Dtos;
using MicroCMS.Application.Features.Components.Queries;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Components;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;
using MicroCMS.Application.Features.Components.Services;

namespace MicroCMS.Application.Features.Components.Handlers;

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class ComponentMapper
{
    private static IReadOnlyList<string> ParseZones(string zonesJson)
    {
        try { return JsonSerializer.Deserialize<List<string>>(zonesJson) ?? []; }
   catch { return []; }
    }

    internal static ComponentDto ToDto(Component c) => new(
      c.Id.Value,
  c.TenantId.Value,
      c.SiteId.Value,
      c.Name,
      c.Key,
      c.Description,
      c.Category,
     ParseZones(c.ZonesJson),
      c.UsageCount,
  c.ItemCount,
  c.TemplateType.ToString(),
      c.TemplateContent,
      c.Fields.Select(f => new ComponentFieldDto(
   f.Id, f.Handle, f.Label, f.FieldType.ToString(),
        f.IsRequired, f.IsLocalized, f.IsUnique, f.SortOrder, f.Description
   )).ToList(),
  c.CreatedAt,
      c.UpdatedAt);

    internal static ComponentListItemDto ToListItemDto(Component c) => new(
   c.Id.Value,
        c.Name,
     c.Key,
        c.Description,
        c.Category,
        ParseZones(c.ZonesJson),
        c.UsageCount,
        c.ItemCount,
        c.Fields.Count,
        c.TemplateType.ToString(),
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
        var fieldTypes = ParseFieldTypes(request.Fields);
        var comp = Component.Create(
  currentUser.TenantId, new SiteId(request.SiteId),
          request.Name, request.Key, request.Description,
            request.Category, request.Zones ?? []);

        if (request.Fields is { Count: > 0 })
        {
 foreach (var (f, ft) in request.Fields.Zip(fieldTypes))
   comp.AddField(f.Handle, f.Label, ft, f.IsRequired, f.Description);
  }

        await repo.AddAsync(comp, cancellationToken);

// Auto-create backing ContentType for this component
     await backingTypeProvisioner.ProvisionAsync(comp, cancellationToken);

        return Result.Success(ComponentMapper.ToDto(comp));
    }

    private static IEnumerable<FieldType> ParseFieldTypes(IReadOnlyList<ComponentFieldInput>? fields)
    {
  if (fields is null) return [];
return fields.Select(f =>
    Enum.TryParse<FieldType>(f.FieldType, true, out var ft) ? ft : FieldType.ShortText);
 }
}

internal sealed class UpdateComponentCommandHandler(
    IRepository<Component, ComponentId> repo)
    : IRequestHandler<UpdateComponentCommand, Result<ComponentDto>>
{
    public async Task<Result<ComponentDto>> Handle(UpdateComponentCommand request, CancellationToken cancellationToken)
    {
  var comp = await repo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);

  comp.Update(request.Name, request.Description, request.Category, request.Zones);

        comp.ReplaceFieldsFromData(request.Fields.Select((f, i) => (
      f.Handle, f.Label,
            Enum.TryParse<FieldType>(f.FieldType, true, out var ft) ? ft : FieldType.ShortText,
            f.IsRequired, f.IsLocalized, f.IsIndexed, i, f.Description)));

     repo.Update(comp);
        return Result.Success(ComponentMapper.ToDto(comp));
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
    IRepository<Component, ComponentId> repo)
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
        return Result.Success(ComponentMapper.ToDto(comp));
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
  IRepository<Component, ComponentId> repo)
    : IRequestHandler<ListComponentsQuery, Result<PagedList<ComponentListItemDto>>>
{
    public async Task<Result<PagedList<ComponentListItemDto>>> Handle(ListComponentsQuery request, CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var items = await repo.ListAsync(new ComponentsBySiteSpec(siteId, request.Page, request.PageSize), cancellationToken);
        var total = await repo.CountAsync(new ComponentsBySiteCountSpec(siteId), cancellationToken);
        return Result.Success(PagedList<ComponentListItemDto>.Create(
            items.Select(ComponentMapper.ToListItemDto),
 request.Page, request.PageSize, total));
    }
}

internal sealed class GetComponentQueryHandler(
    IRepository<Component, ComponentId> repo)
    : IRequestHandler<GetComponentQuery, Result<ComponentDto>>
{
    public async Task<Result<ComponentDto>> Handle(GetComponentQuery request, CancellationToken cancellationToken)
    {
        var comp = await repo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);
        return Result.Success(ComponentMapper.ToDto(comp));
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
