using MediatR;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Folders.Queries;

/// <summary>Handles <see cref="GetFolderTreeQuery"/>.</summary>
internal sealed class GetFolderTreeQueryHandler(IRepository<Folder, FolderId> folderRepository)
    : IRequestHandler<GetFolderTreeQuery, Result<IReadOnlyList<FolderTreeNode>>>
{
    public async Task<Result<IReadOnlyList<FolderTreeNode>>> Handle(
    GetFolderTreeQuery request,
        CancellationToken cancellationToken)
    {
      var spec = new FoldersBySiteSpec(new SiteId(request.SiteId));
        var folders = await folderRepository.ListAsync(spec, cancellationToken);

        var roots = BuildTree(folders, parentId: null);
      return Result.Success(roots);
    }

    private static IReadOnlyList<FolderTreeNode> BuildTree(
        IReadOnlyList<Folder> all,
        FolderId? parentId)
    {
        return all
            .Where(f => f.ParentFolderId == parentId)
  .Select(f => new FolderTreeNode(
          f.Id.Value,
          f.Name,
                f.ParentFolderId?.Value,
       BuildTree(all, f.Id)))
            .ToList()
      .AsReadOnly();
    }
}
