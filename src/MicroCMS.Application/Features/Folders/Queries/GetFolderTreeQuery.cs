using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;

namespace MicroCMS.Application.Features.Folders.Queries;

/// <summary>Returns the full folder tree for a site as a nested list (GAP-02).</summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record GetFolderTreeQuery(Guid SiteId) : IQuery<IReadOnlyList<FolderTreeNode>>;

/// <summary>Represents a single node in the folder tree; children are nested recursively.</summary>
public sealed record FolderTreeNode(
    Guid Id,
    string Name,
  Guid? ParentFolderId,
    IReadOnlyList<FolderTreeNode> Children);
