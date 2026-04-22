using FluentValidation.Results;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.ContentTypes.Commands;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Application.Features.ContentTypes.Mappers;
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
    IRepository<ContentType, ContentTypeId> repo)
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
    return Result.Success(ContentTypeMapper.ToDto(ct));
    }
}

internal sealed class RemoveFieldCommandHandler(
    IRepository<ContentType, ContentTypeId> repo)
    : IRequestHandler<RemoveFieldCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(RemoveFieldCommand request, CancellationToken cancellationToken)
    {
        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
  ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

   ct.RemoveField(request.FieldId);
        repo.Update(ct);
        return Result.Success(ContentTypeMapper.ToDto(ct));
    }
}

internal sealed class PublishContentTypeCommandHandler(
    IRepository<ContentType, ContentTypeId> repo)
    : IRequestHandler<PublishContentTypeCommand, Result<ContentTypeDto>>
{
 public async Task<Result<ContentTypeDto>> Handle(PublishContentTypeCommand request, CancellationToken cancellationToken)
    {
        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
 ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        ct.Publish();
        repo.Update(ct);
        return Result.Success(ContentTypeMapper.ToDto(ct));
    }
}

internal sealed class ArchiveContentTypeCommandHandler(
IRepository<ContentType, ContentTypeId> repo)
    : IRequestHandler<ArchiveContentTypeCommand, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(ArchiveContentTypeCommand request, CancellationToken cancellationToken)
    {
   var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
  ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        ct.Archive();
        repo.Update(ct);
  return Result.Success(ContentTypeMapper.ToDto(ct));
    }
}
