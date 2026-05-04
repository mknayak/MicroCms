using MediatR;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Folders.Queries;

/// <summary>Handles <see cref="GetFolderTreeQuery"/>.</summary>
internal sealed class GetFolderTreeQueryHandler(
    IRepository<Folder, FolderId> folderRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetFolderTreeQuery, Result<IReadOnlyList<FolderTreeNode>>>
{
    public async Task<Result<IReadOnlyList<FolderTreeNode>>> Handle(
    GetFolderTreeQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<IReadOnlyList<FolderTreeNode>>(Error.Validation("Auth.NoSiteContext", "No site context in token. Call POST /auth/switch-site first."));

        var spec = new FoldersBySiteSpec(siteId);
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
