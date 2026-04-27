using FluentValidation.Results;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.ContentTypes.Commands;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Application.Features.ContentTypes.Mappers;
using MicroCMS.Application.Features.Search.EventHandlers;
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
        var ct = ContentType.Create(
                currentUser.TenantId,
      new SiteId(request.SiteId),
          request.Handle,
          request.DisplayName,
      request.Description);

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
        request.IsRequired, request.IsLocalized, request.IsUnique, request.Description);

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
    ICacheService cacheService)
    : IRequestHandler<UpdateContentTypeCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(UpdateContentTypeCommand request, CancellationToken cancellationToken)
    {
        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
           ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        ct.Update(request.DisplayName, request.Description);

        if (request.Fields is not null)
        {
            // Remove fields not present in the incoming list
            var incomingIds = request.Fields
                        .Where(f => f.Id.HasValue)
                 .Select(f => f.Id!.Value)
            .ToHashSet();

            foreach (var existing in ct.Fields.Where(f => !incomingIds.Contains(f.Id)).ToList())
                ct.RemoveField(existing.Id);

            foreach (var f in request.Fields)
            {
                if (!Enum.TryParse<FieldType>(f.FieldType, ignoreCase: true, out var fieldType))
                    throw new ValidationException([new ValidationFailure("FieldType", $"'{f.FieldType}' is not a valid FieldType.")]);

                if (f.Id.HasValue)
                    ct.UpdateField(f.Id.Value, f.Label, fieldType, f.IsRequired, f.IsLocalized, f.SortOrder, f.Description);
                else
                    ct.AddField(f.Handle, f.Label, fieldType, f.IsRequired, f.IsLocalized, f.IsUnique, f.Description);
            }
        }

        repo.Update(ct);
 await InvalidateAsync(ct.TenantId, ct.Id.Value, cancellationToken);
   return Result.Success(ContentTypeMapper.ToDto(ct));
    }

    private Task InvalidateAsync(TenantId tenantId, Guid id, CancellationToken ct) => Task.WhenAll(
    cacheService.RemoveAsync(CacheKeys.ContentType(tenantId, id), ct),
    cacheService.RemoveByTagAsync(CacheTags.TenantContentTypes(tenantId), ct));
}

internal sealed class DeleteContentTypeCommandHandler(
    IRepository<ContentType, ContentTypeId> repo,
    ICacheService cacheService)
    : IRequestHandler<DeleteContentTypeCommand, Result>
{
    public async Task<Result> Handle(DeleteContentTypeCommand request, CancellationToken cancellationToken)
    {
        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
      ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        var tenantId = ct.TenantId;
        var id = ct.Id.Value;
        repo.Remove(ct);
        await InvalidateAsync(tenantId, id, cancellationToken);
        return Result.Success();
    }

    private Task InvalidateAsync(TenantId tenantId, Guid id, CancellationToken ct) => Task.WhenAll(
        cacheService.RemoveAsync(CacheKeys.ContentType(tenantId, id), ct),
      cacheService.RemoveByTagAsync(CacheTags.TenantContentTypes(tenantId), ct));
}
