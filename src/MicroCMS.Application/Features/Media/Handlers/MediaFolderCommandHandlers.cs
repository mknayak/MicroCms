using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Media.Commands;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Application.Features.Media.Mappers;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Media;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Media.Handlers;

public sealed class CreateMediaFolderCommandHandler(
    IRepository<MediaFolder, Guid> repo,
    ICurrentUser currentUser) : IRequestHandler<CreateMediaFolderCommand, Result<MediaFolderDto>>
{
    public async Task<Result<MediaFolderDto>> Handle(
        CreateMediaFolderCommand request,
        CancellationToken cancellationToken)
    {
        var folder = MediaFolderFactory.Create(
            currentUser.TenantId,
            new SiteId(request.SiteId),
            request.Name,
            request.ParentFolderId);

        await repo.AddAsync(folder, cancellationToken);
        return Result.Success(MediaMapper.ToFolderDto(folder));
    }
}

public sealed class RenameMediaFolderCommandHandler(
    IRepository<MediaFolder, Guid> repo) : IRequestHandler<RenameMediaFolderCommand, Result<MediaFolderDto>>
{
    public async Task<Result<MediaFolderDto>> Handle(
        RenameMediaFolderCommand request,
        CancellationToken cancellationToken)
    {
        var folder = await repo.GetByIdAsync(request.FolderId, cancellationToken)
            ?? throw new NotFoundException(nameof(MediaFolder), request.FolderId);

        MediaFolderFactory.Rename(folder, request.NewName);
        repo.Update(folder);
        return Result.Success(MediaMapper.ToFolderDto(folder));
    }
}

public sealed class MoveMediaFolderCommandHandler(
    IRepository<MediaFolder, Guid> repo) : IRequestHandler<MoveMediaFolderCommand, Result<MediaFolderDto>>
{
    public async Task<Result<MediaFolderDto>> Handle(
        MoveMediaFolderCommand request,
        CancellationToken cancellationToken)
    {
        var folder = await repo.GetByIdAsync(request.FolderId, cancellationToken)
            ?? throw new NotFoundException(nameof(MediaFolder), request.FolderId);

        MediaFolderFactory.Move(folder, request.NewParentFolderId);
        repo.Update(folder);
        return Result.Success(MediaMapper.ToFolderDto(folder));
    }
}

public sealed class DeleteMediaFolderCommandHandler(
    IRepository<MediaFolder, Guid> folderRepo,
    IRepository<MediaAsset, MediaAssetId> assetRepo) : IRequestHandler<DeleteMediaFolderCommand, Result>
{
    public async Task<Result> Handle(DeleteMediaFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await folderRepo.GetByIdAsync(request.FolderId, cancellationToken)
            ?? throw new NotFoundException(nameof(MediaFolder), request.FolderId);

        var childFolders = await folderRepo.ListAsync(
            new ChildMediaFoldersSpec(request.FolderId), cancellationToken);

        if (childFolders.Count > 0)
            return Result.Failure(Error.Conflict(
                "MediaFolder.HasChildren",
                $"Folder '{folder.Name}' has {childFolders.Count} child folder(s). Remove them first."));

        var assets = await assetRepo.ListAsync(
            new MediaAssetsByFolderSpec(request.FolderId), cancellationToken);

        if (assets.Count > 0)
            return Result.Failure(Error.Conflict(
                "MediaFolder.NotEmpty",
                $"Folder '{folder.Name}' contains {assets.Count} asset(s). Move or delete them first."));

        folderRepo.Remove(folder);
        return Result.Success();
    }
}
