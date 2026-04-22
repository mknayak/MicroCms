using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;

namespace MicroCMS.Application.Features.Folders.Commands;

/// <summary>Creates a new content folder within a site (GAP-02).</summary>
[HasPolicy(ContentPolicies.FolderManage)]
public sealed record CreateFolderCommand(
 Guid SiteId,
    string Name,
  Guid? ParentFolderId = null) : ICommand<FolderDto>;

/// <summary>Renames an existing folder (GAP-02).</summary>
[HasPolicy(ContentPolicies.FolderManage)]
public sealed record RenameFolderCommand(Guid FolderId, string NewName) : ICommand<FolderDto>;

/// <summary>Moves a folder under a different parent (or to the root when ParentFolderId is null) (GAP-02).</summary>
[HasPolicy(ContentPolicies.FolderManage)]
public sealed record MoveFolderCommand(Guid FolderId, Guid? NewParentFolderId) : ICommand<FolderDto>;

/// <summary>Deletes an empty folder. Non-empty folders must have entries moved first (GAP-02).</summary>
[HasPolicy(ContentPolicies.FolderManage)]
public sealed record DeleteFolderCommand(Guid FolderId) : ICommand;

/// <summary>Moves an entry into a folder (or to the site root when FolderId is null) (GAP-02).</summary>
[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record MoveEntryToFolderCommand(Guid EntryId, Guid? FolderId) : ICommand;

public sealed record FolderDto(
    Guid Id,
    Guid SiteId,
    string Name,
    Guid? ParentFolderId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
