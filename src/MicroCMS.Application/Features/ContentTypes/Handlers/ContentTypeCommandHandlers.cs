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

// ── Validation JSON builder helper ────────────────────────────────────────────

internal static class ValidationJsonHelper
{
    /// <summary>
    /// Builds the <see cref="FieldValidationConfig"/> JSON string from command inputs.
    /// Returns null when no validation config is present (non-Enum fields with no constraints).
    /// </summary>
    internal static string? Build(
        IReadOnlyList<string>? options,
        FieldDynamicSourceInput? dynamicSource,
        FieldDynamicSourceInput? multiListSource = null,
        string? componentSourceKey = null)
    {
        if (options is null && dynamicSource is null && multiListSource is null && componentSourceKey is null)
            return null;

        var config = new FieldValidationConfig
        {
            Options = options,
            DynamicSource = dynamicSource is null ? null : new FieldDynamicSource
            {
                ContentTypeHandle = dynamicSource.ContentTypeHandle,
                LabelField = dynamicSource.LabelField,
                ValueField = dynamicSource.ValueField,
                StatusFilter = dynamicSource.StatusFilter,
                GroupHandle = dynamicSource.GroupHandle,
            },
            MultiListSource = multiListSource is null ? null : new FieldDynamicSource
            {
                ContentTypeHandle = multiListSource.ContentTypeHandle,
                LabelField = multiListSource.LabelField,
                ValueField = multiListSource.ValueField,
                StatusFilter = multiListSource.StatusFilter,
                GroupHandle = multiListSource.GroupHandle,
            },
            ComponentSource = componentSourceKey is null ? null : new ComponentFieldSource
            {
                ComponentKey = componentSourceKey,
            },
        };
        return config.ToJson();
    }
}

// ── Handlers ──────────────────────────────────────────────────────────────────

internal sealed class CreateContentTypeCommandHandler(
    IRepository<ContentType, ContentTypeId> repo,
    ICurrentUser currentUser,
    ICacheService cacheService)
    : IRequestHandler<CreateContentTypeCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(CreateContentTypeCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<ContentTypeDto>(Error.Validation("Auth.NoSiteContext", "No site context in token. Call POST /auth/switch-site first."));

        var kind = Enum.TryParse<ContentTypeKind>(request.Kind, ignoreCase: true, out var k)
            ? k : ContentTypeKind.Content;
        var ct = ContentType.Create(
      currentUser.TenantId, siteId,
            request.Handle, request.DisplayName, request.Description,
            request.Localization, kind);

        if (request.ParentContentTypeId.HasValue)
        {
            var parent = await repo.GetByIdAsync(new ContentTypeId(request.ParentContentTypeId.Value), cancellationToken)
                ?? throw new NotFoundException(nameof(ContentType), request.ParentContentTypeId.Value);
            ct.SetParent(parent);
        }

   await repo.AddAsync(ct, cancellationToken);
        await cacheService.RemoveByTagAsync(CacheTags.TenantContentTypes(currentUser.TenantId), cancellationToken);
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

        var validationJson = ValidationJsonHelper.Build(request.Options, request.DynamicSource, request.MultiListSource, request.ComponentSourceKey);

        ct.AddField(request.Handle, request.Label, fieldType,
       request.IsRequired, request.IsLocalized, request.IsUnique,
       description: request.Description,
      validationJson: validationJson,
    isIndexed: request.IsIndexed,
            isList: request.IsList,
            groupName: request.GroupName);

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

        ContentType? parent = null;
        if (ct.ParentContentTypeId is { } parentId)
            parent = await repo.GetByIdAsync(parentId, cancellationToken);

        ct.EnsureParentIsActive(parent);
        ct.Publish();
  repo.Update(ct);
        await InvalidateAsync(ct.TenantId, ct.Id.Value, cancellationToken);
        return Result.Success(ContentTypeMapper.ToDto(ct, parent));
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
    IRepository<SiteTemplate, SiteTemplateId> siteTemplateRepo,
ICacheService cacheService)
    : IRequestHandler<UpdateContentTypeCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(UpdateContentTypeCommand request, CancellationToken cancellationToken)
    {
        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
 ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

      ct.Update(request.DisplayName, request.Description, request.Localization);
    await ApplyKindAndLayout(ct, request, siteTemplateRepo, cancellationToken);

        ContentType? parent = null;
        if (request.ClearParent)
        {
            ct.SetParent(null);
        }
        else if (request.ParentContentTypeId.HasValue)
        {
            parent = await repo.GetByIdAsync(new ContentTypeId(request.ParentContentTypeId.Value), cancellationToken)
                ?? throw new NotFoundException(nameof(ContentType), request.ParentContentTypeId.Value);
            ct.SetParent(parent);
        }
        else if (ct.ParentContentTypeId is { } existingParentId)
        {
            // Parent unchanged — load for the mapper
            parent = await repo.GetByIdAsync(existingParentId, cancellationToken);
        }

        if (request.Fields is not null) ApplyFieldUpdates(ct, request.Fields);

 repo.Update(ct);
        await InvalidateAsync(ct.TenantId, ct.Id.Value, cancellationToken);
        return Result.Success(ContentTypeMapper.ToDto(ct, parent));
    }

    private static async Task ApplyKindAndLayout(
        ContentType ct, UpdateContentTypeCommand request,
        IRepository<SiteTemplate, SiteTemplateId> siteTemplateRepo,
        CancellationToken cancellationToken)
    {
        if (request.Kind is not null &&
            Enum.TryParse<ContentTypeKind>(request.Kind, ignoreCase: true, out var kind))
            ct.SetKind(kind);

        if (request.SiteTemplateId.HasValue)
        {
            var template = await siteTemplateRepo.GetByIdAsync(new SiteTemplateId(request.SiteTemplateId.Value), cancellationToken)
                ?? throw new NotFoundException(nameof(SiteTemplate), request.SiteTemplateId.Value);
            ct.SetSiteTemplate(template.Id);
        }
        else if (request.SiteTemplateId == null && ct.Kind == ContentTypeKind.Page)
        {
            // Explicit null clears the template
            ct.SetSiteTemplate(null);
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

        var validationJson = ValidationJsonHelper.Build(f.Options, f.DynamicSource, f.MultiListSource, f.ComponentSourceKey);

        if (f.Id.HasValue)
        ct.UpdateField(f.Id.Value, f.Label, fieldType,
   f.IsRequired, f.IsLocalized, f.IsIndexed, f.IsList,
            f.SortOrder, f.Description, validationJson, f.GroupName);
        else
            ct.AddField(f.Handle, f.Label, fieldType,
           f.IsRequired, f.IsLocalized, f.IsUnique,
             description: f.Description,
   validationJson: validationJson,
       isIndexed: f.IsIndexed,
           isList: f.IsList,
           groupName: f.GroupName);
    }

    private Task InvalidateAsync(TenantId tenantId, Guid id, CancellationToken ct) => Task.WhenAll(
cacheService.RemoveAsync(CacheKeys.ContentType(tenantId, id), ct),
        cacheService.RemoveByTagAsync(CacheTags.TenantContentTypes(tenantId), ct));
}
