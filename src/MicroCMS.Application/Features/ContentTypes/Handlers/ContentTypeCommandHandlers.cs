using FluentValidation.Results;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.ContentTypes.Commands;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Application.Features.ContentTypes.Mappers;
using MicroCMS.Application.Features.Search.EventHandlers;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.ContentTypes.Handlers;

internal sealed class CreateContentTypeCommandHandler(
    IRepository<ContentType, ContentTypeId> repo,
    ICurrentUser currentUser)
    : IRequestHandler<CreateContentTypeCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(CreateContentTypeCommand request, CancellationToken cancellationToken)
    {
        var kind = Enum.TryParse<ContentTypeKind>(request.Kind, ignoreCase: true, out var k)
       ? k : ContentTypeKind.Content;

        var ct = ContentType.Create(
         currentUser.TenantId, new SiteId(request.SiteId),
        request.Handle, request.DisplayName, request.Description,
       request.Localization, kind);

        await repo.AddAsync(ct, cancellationToken);
        return Result.Success(ContentTypeMapper.ToDto(ct));
    }
}

internal sealed class AddFieldCommandHandler(
    IRepository<ContentType, ContentTypeId> repo,
    ICacheService cacheService)
    : IRequestHandler<AddFieldCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(AddFieldCommand request, CancellationToken cancellationToken)
    {
        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
      ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        if (!Enum.TryParse<FieldType>(request.FieldType, ignoreCase: true, out var fieldType))
            throw new ValidationException([new ValidationFailure("FieldType", $"'{request.FieldType}' is not a valid FieldType.")]);

        ct.AddField(request.Handle, request.Label, fieldType,
            request.IsRequired, request.IsLocalized, request.IsUnique,
            description: request.Description, isIndexed: request.IsIndexed);

        repo.Update(ct);
        await InvalidateAsync(ct.TenantId, ct.Id.Value, cancellationToken);
        return Result.Success(ContentTypeMapper.ToDto(ct));
    }

    private Task InvalidateAsync(TenantId tenantId, Guid id, CancellationToken ct) => Task.WhenAll(
        cacheService.RemoveAsync(CacheKeys.ContentType(tenantId, id), ct),
    cacheService.RemoveByTagAsync(CacheTags.TenantContentTypes(tenantId), ct));
}

internal sealed class RemoveFieldCommandHandler(
    IRepository<ContentType, ContentTypeId> repo,
    ICacheService cacheService)
    : IRequestHandler<RemoveFieldCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(RemoveFieldCommand request, CancellationToken cancellationToken)
    {
        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
  ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        ct.RemoveField(request.FieldId);
        repo.Update(ct);
        await InvalidateAsync(ct.TenantId, ct.Id.Value, cancellationToken);
        return Result.Success(ContentTypeMapper.ToDto(ct));
    }

    private Task InvalidateAsync(TenantId tenantId, Guid id, CancellationToken ct) => Task.WhenAll(
        cacheService.RemoveAsync(CacheKeys.ContentType(tenantId, id), ct),
        cacheService.RemoveByTagAsync(CacheTags.TenantContentTypes(tenantId), ct));
}

internal sealed class PublishContentTypeCommandHandler(
    IRepository<ContentType, ContentTypeId> repo,
    ICacheService cacheService)
    : IRequestHandler<PublishContentTypeCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(PublishContentTypeCommand request, CancellationToken cancellationToken)
    {
        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
 ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        ct.Publish();
        repo.Update(ct);
        await InvalidateAsync(ct.TenantId, ct.Id.Value, cancellationToken);
        return Result.Success(ContentTypeMapper.ToDto(ct));
    }

    private Task InvalidateAsync(TenantId tenantId, Guid id, CancellationToken ct) => Task.WhenAll(
      cacheService.RemoveAsync(CacheKeys.ContentType(tenantId, id), ct),
          cacheService.RemoveByTagAsync(CacheTags.TenantContentTypes(tenantId), ct));
}

internal sealed class ArchiveContentTypeCommandHandler(
    IRepository<ContentType, ContentTypeId> repo,
    ICacheService cacheService)
  : IRequestHandler<ArchiveContentTypeCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(ArchiveContentTypeCommand request, CancellationToken cancellationToken)
    {
        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
   ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);
        ct.Archive();
        repo.Update(ct);
        await InvalidateAsync(ct.TenantId, ct.Id.Value, cancellationToken);
        return Result.Success(ContentTypeMapper.ToDto(ct));
    }

    private Task InvalidateAsync(TenantId tenantId, Guid id, CancellationToken ct) => Task.WhenAll(
          cacheService.RemoveAsync(CacheKeys.ContentType(tenantId, id), ct),
          cacheService.RemoveByTagAsync(CacheTags.TenantContentTypes(tenantId), ct));
}

internal sealed class UpdateContentTypeCommandHandler(
    IRepository<ContentType, ContentTypeId> repo,
 IRepository<Layout, LayoutId> layoutRepo,
    ICacheService cacheService)
    : IRequestHandler<UpdateContentTypeCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(UpdateContentTypeCommand request, CancellationToken cancellationToken)
    {
        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
     ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        ct.Update(request.DisplayName, request.Description, request.Localization);
        await ApplyKindAndLayout(ct, request, layoutRepo, cancellationToken);
        if (request.Fields is not null) ApplyFieldUpdates(ct, request.Fields);

        repo.Update(ct);
        await InvalidateAsync(ct.TenantId, ct.Id.Value, cancellationToken);
        return Result.Success(ContentTypeMapper.ToDto(ct));
    }

    private static async Task ApplyKindAndLayout(
        ContentType ct, UpdateContentTypeCommand request,
        IRepository<Layout, LayoutId> layoutRepo, CancellationToken cancellationToken)
    {
        if (request.Kind is not null &&
          Enum.TryParse<ContentTypeKind>(request.Kind, ignoreCase: true, out var kind))
            ct.SetKind(kind);

        if (request.LayoutId.HasValue)
        {
            var layout = await layoutRepo.GetByIdAsync(new LayoutId(request.LayoutId.Value), cancellationToken)
       ?? throw new NotFoundException(nameof(Layout), request.LayoutId.Value);
            ct.SetLayout(layout.Id);
        }
    }

    private static void ApplyFieldUpdates(ContentType ct, IReadOnlyList<UpdateFieldInput> fields)
    {
        var incomingIds = fields.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToHashSet();
        foreach (var existing in ct.Fields.Where(f => !incomingIds.Contains(f.Id)).ToList())
            ct.RemoveField(existing.Id);

        foreach (var f in fields)
            ApplySingleField(ct, f);
    }

    private static void ApplySingleField(ContentType ct, UpdateFieldInput f)
    {
        if (!Enum.TryParse<FieldType>(f.FieldType, ignoreCase: true, out var fieldType))
            throw new ValidationException([new ValidationFailure("FieldType", $"'{f.FieldType}' is not a valid FieldType.")]);

        if (f.Id.HasValue)
            ct.UpdateField(f.Id.Value, f.Label, fieldType, f.IsRequired, f.IsLocalized, f.IsIndexed, f.SortOrder, f.Description);
        else
            ct.AddField(f.Handle, f.Label, fieldType, f.IsRequired, f.IsLocalized, f.IsUnique,
             description: f.Description, isIndexed: f.IsIndexed);
    }

    private Task InvalidateAsync(TenantId tenantId, Guid id, CancellationToken ct) => Task.WhenAll(
           cacheService.RemoveAsync(CacheKeys.ContentType(tenantId, id), ct),
           cacheService.RemoveByTagAsync(CacheTags.TenantContentTypes(tenantId), ct));
}

/// <summary>Sets or clears the layout on a Page-kind content type.</summary>
internal sealed class SetContentTypeLayoutCommandHandler(
    IRepository<ContentType, ContentTypeId> repo,
    IRepository<Layout, LayoutId> layoutRepo,
 ICacheService cacheService)
    : IRequestHandler<SetContentTypeLayoutCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(SetContentTypeLayoutCommand request, CancellationToken cancellationToken)
    {
        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
    ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        LayoutId? layoutId = null;
        if (request.LayoutId.HasValue)
        {
            var layout = await layoutRepo.GetByIdAsync(new LayoutId(request.LayoutId.Value), cancellationToken)
     ?? throw new NotFoundException(nameof(Layout), request.LayoutId.Value);
            layoutId = layout.Id;
        }

        ct.SetLayout(layoutId);
        repo.Update(ct);
        await Task.WhenAll(
       cacheService.RemoveAsync(CacheKeys.ContentType(ct.TenantId, ct.Id.Value), cancellationToken),
        cacheService.RemoveByTagAsync(CacheTags.TenantContentTypes(ct.TenantId), cancellationToken));

        return Result.Success(ContentTypeMapper.ToDto(ct));
    }
}
