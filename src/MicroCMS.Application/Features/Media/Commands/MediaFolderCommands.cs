using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Media.Dtos;

namespace MicroCMS.Application.Features.Media.Commands;

/// <summary>Creates a new folder in the media library hierarchy.</summary>
[HasPolicy(ContentPolicies.MediaUpload)]
public sealed record CreateMediaFolderCommand(
    Guid SiteId,
    string Name,
    Guid? ParentFolderId = null) : ICommand<MediaFolderDto>;

/// <summary>Renames an existing media folder.</summary>
[HasPolicy(ContentPolicies.MediaUpload)]
public sealed record RenameMediaFolderCommand(
    Guid FolderId,
    string NewName) : ICommand<MediaFolderDto>;

/// <summary>Moves a media folder under a new parent (null = root).</summary>
[HasPolicy(ContentPolicies.MediaUpload)]
public sealed record MoveMediaFolderCommand(
    Guid FolderId,
    Guid? NewParentFolderId) : ICommand<MediaFolderDto>;

/// <summary>
/// Deletes a media folder. Fails when the folder still contains assets.
/// </summary>
[HasPolicy(ContentPolicies.MediaDelete)]
public sealed record DeleteMediaFolderCommand(Guid FolderId) : ICommand;
