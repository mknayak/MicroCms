using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Folders.Commands;

internal static class FolderMapper
{
    internal static FolderDto ToDto(Folder f) => new(
  f.Id.Value,
        f.SiteId.Value,
        f.Name,
        f.ParentFolderId?.Value,
        f.CreatedAt,
 f.UpdatedAt);
}

internal sealed class CreateFolderCommandHandler(
    IRepository<Folder, FolderId> folderRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CreateFolderCommand, Result<FolderDto>>
{
    public async Task<Result<FolderDto>> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
   var folder = Folder.Create(
          currentUser.TenantId,
       new SiteId(request.SiteId),
 request.Name,
   request.ParentFolderId.HasValue ? new FolderId(request.ParentFolderId.Value) : null);

  await folderRepository.AddAsync(folder, cancellationToken);
        return Result.Success(FolderMapper.ToDto(folder));
    }
}

internal sealed class RenameFolderCommandHandler(IRepository<Folder, FolderId> folderRepository)
    : IRequestHandler<RenameFolderCommand, Result<FolderDto>>
{
public async Task<Result<FolderDto>> Handle(RenameFolderCommand request, CancellationToken cancellationToken)
    {
   var folder = await folderRepository.GetByIdAsync(new FolderId(request.FolderId), cancellationToken)
 ?? throw new NotFoundException(nameof(Folder), request.FolderId);

        folder.Rename(request.NewName);
   folderRepository.Update(folder);
    return Result.Success(FolderMapper.ToDto(folder));
    }
}

internal sealed class MoveFolderCommandHandler(IRepository<Folder, FolderId> folderRepository)
    : IRequestHandler<MoveFolderCommand, Result<FolderDto>>
{
    public async Task<Result<FolderDto>> Handle(MoveFolderCommand request, CancellationToken cancellationToken)
    {
     var folder = await folderRepository.GetByIdAsync(new FolderId(request.FolderId), cancellationToken)
    ?? throw new NotFoundException(nameof(Folder), request.FolderId);

      folder.MoveTo(request.NewParentFolderId.HasValue ? new FolderId(request.NewParentFolderId.Value) : null);
  folderRepository.Update(folder);
        return Result.Success(FolderMapper.ToDto(folder));
    }
}

internal sealed class DeleteFolderCommandHandler(
    IRepository<Folder, FolderId> folderRepository,
    IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<DeleteFolderCommand, Result>
{
    public async Task<Result> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
    {
  var folderId = new FolderId(request.FolderId);
  var folder = await folderRepository.GetByIdAsync(folderId, cancellationToken)
        ?? throw new NotFoundException(nameof(Folder), request.FolderId);

        // Guard: refuse delete if entries still live in this folder
      var entriesInFolder = await entryRepository.ListAsync(
   new EntriesByFolderSpec(folderId), cancellationToken);

    if (entriesInFolder.Count > 0)
  return Result.Failure(Error.Conflict(
         "Folder.NotEmpty",
          $"Folder '{folder.Name}' contains {entriesInFolder.Count} entries. Move them before deleting."));

        folderRepository.Remove(folder);
        return Result.Success();
    }
}

internal sealed class MoveEntryToFolderCommandHandler(IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<MoveEntryToFolderCommand, Result>
{
    public async Task<Result> Handle(MoveEntryToFolderCommand request, CancellationToken cancellationToken)
    {
   var entry = await entryRepository.GetByIdAsync(new EntryId(request.EntryId), cancellationToken)
   ?? throw new NotFoundException(nameof(Entry), request.EntryId);

  entry.MoveToFolder(request.FolderId.HasValue ? new FolderId(request.FolderId.Value) : null);
 entryRepository.Update(entry);
        return Result.Success();
    }
}
