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
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.ValueObjects;
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
        backingType.Fields.OrderBy(f => f.SortOrder).Select(f =>
        {
            var v = f.Validation;
            return new ComponentFieldDto(
                f.Id, f.Handle, f.Label, f.FieldType.ToString(),
                f.IsRequired, f.IsLocalized, f.IsUnique, f.IsIndexed, f.IsList, f.SortOrder, f.Description,
                f.GroupName,
                Options: v?.Options,
                DynamicSource: v?.DynamicSource,
                MultiListSource: v?.MultiListSource);
        }).ToList(),
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

    internal static ComponentItemDto ToItemDto(Entry entry, Component comp) => new(
        entry.Id.Value,
        comp.Id.Value,
        comp.Name,
        comp.Key,
        entry.TenantId.Value,
        entry.SiteId.Value,
        entry.Slug.Value,
        entry.Status.ToString(),
        ParseJson(entry.FieldsJson),
        entry.CreatedAt,
        entry.UpdatedAt);

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

            var validationJson = ContentTypes.Handlers.ValidationJsonHelper.Build(f.Options?.ToList(), f.DynamicSource, f.MultiListSource);

            if (existing is null)
                contentType.AddField(f.Handle, f.Label, ft, f.IsRequired, f.IsLocalized, f.IsUnique, f.Description, validationJson, f.IsIndexed, f.IsList);
            else
                contentType.UpdateField(existing.Id, f.Label, ft, f.IsRequired, f.IsLocalized, f.IsIndexed, f.IsList, i, f.Description, validationJson);
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
    IRepository<Entry, EntryId> entryRepo,
    ICurrentUser currentUser)
    : IRequestHandler<CreateComponentItemCommand, Result<ComponentItemDto>>
{
    public async Task<Result<ComponentItemDto>> Handle(CreateComponentItemCommand request, CancellationToken cancellationToken)
    {
        var comp = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);

        if (comp.BackingContentTypeId is null)
            throw new NotFoundException(nameof(ContentType), request.ComponentId);

        var slug = Slug.Create(request.Slug);
        var locale = Locale.Create("en");
        var entry = Entry.Create(comp.TenantId, comp.SiteId, comp.BackingContentTypeId.Value,
            slug, locale, currentUser.UserId, request.FieldsJson);

        await entryRepo.AddAsync(entry, cancellationToken);

        comp.IncrementItemCount();
        compRepo.Update(comp);

        return Result.Success(ComponentMapper.ToItemDto(entry, comp));
    }
}

internal sealed class UpdateComponentItemCommandHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<Entry, EntryId> entryRepo,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateComponentItemCommand, Result<ComponentItemDto>>
{
    public async Task<Result<ComponentItemDto>> Handle(UpdateComponentItemCommand request, CancellationToken cancellationToken)
    {
        var comp = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);
        var entry = await entryRepo.GetByIdAsync(new EntryId(request.ItemId), cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), request.ItemId);

        entry.UpdateFields(request.FieldsJson, currentUser.UserId);
        entryRepo.Update(entry);
        return Result.Success(ComponentMapper.ToItemDto(entry, comp));
    }
}

internal sealed class PublishComponentItemCommandHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<Entry, EntryId> entryRepo)
    : IRequestHandler<PublishComponentItemCommand, Result>
{
    public async Task<Result> Handle(PublishComponentItemCommand request, CancellationToken cancellationToken)
    {
        _ = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);
        var entry = await entryRepo.GetByIdAsync(new EntryId(request.ItemId), cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), request.ItemId);

        // Entries require Approved state before Publish — auto-approve for component items
        if (entry.Status == EntryStatus.Draft)
            entry.Submit();
        if (entry.Status == EntryStatus.PendingReview)
            entry.Approve();
        entry.Publish();
        entryRepo.Update(entry);
        return Result.Success();
    }
}

internal sealed class ArchiveComponentItemCommandHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<Entry, EntryId> entryRepo)
    : IRequestHandler<ArchiveComponentItemCommand, Result>
{
    public async Task<Result> Handle(ArchiveComponentItemCommand request, CancellationToken cancellationToken)
    {
        _ = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);
        var entry = await entryRepo.GetByIdAsync(new EntryId(request.ItemId), cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), request.ItemId);

        entry.Archive();
        entryRepo.Update(entry);
        return Result.Success();
    }
}

internal sealed class DeleteComponentItemCommandHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<Entry, EntryId> entryRepo)
    : IRequestHandler<DeleteComponentItemCommand, Result>
{
    public async Task<Result> Handle(DeleteComponentItemCommand request, CancellationToken cancellationToken)
    {
        var comp = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);
        var entry = await entryRepo.GetByIdAsync(new EntryId(request.ItemId), cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), request.ItemId);

        entryRepo.Remove(entry);
        comp.DecrementItemCount();
        compRepo.Update(comp);
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
    IRepository<Entry, EntryId> entryRepo)
    : IRequestHandler<ListComponentItemsQuery, Result<PagedList<ComponentItemDto>>>
{
    public async Task<Result<PagedList<ComponentItemDto>>> Handle(ListComponentItemsQuery request, CancellationToken cancellationToken)
    {
        var comp = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);

        if (comp.BackingContentTypeId is null)
            return Result.Success(PagedList<ComponentItemDto>.Create([], request.Page, request.PageSize, 0));

        var contentTypeId = comp.BackingContentTypeId.Value.Value;

        var items = await entryRepo.ListAsync(
            new EntriesBySiteSpec(comp.SiteId, request.Status, contentTypeId, null, null, request.Page, request.PageSize),
            cancellationToken);
        var total = await entryRepo.CountAsync(
            new EntriesBySiteSpec(comp.SiteId, request.Status, contentTypeId),
            cancellationToken);

        return Result.Success(PagedList<ComponentItemDto>.Create(
            items.Select(e => ComponentMapper.ToItemDto(e, comp)),
            request.Page, request.PageSize, total));
    }
}

internal sealed class GetComponentItemQueryHandler(
    IRepository<Component, ComponentId> compRepo,
    IRepository<Entry, EntryId> entryRepo)
    : IRequestHandler<GetComponentItemQuery, Result<ComponentItemDto>>
{
    public async Task<Result<ComponentItemDto>> Handle(GetComponentItemQuery request, CancellationToken cancellationToken)
    {
        var comp = await compRepo.GetByIdAsync(new ComponentId(request.ComponentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Component), request.ComponentId);
        var entry = await entryRepo.GetByIdAsync(new EntryId(request.ItemId), cancellationToken)
            ?? throw new NotFoundException(nameof(Entry), request.ItemId);
        return Result.Success(ComponentMapper.ToItemDto(entry, comp));
    }
}
