using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Media.Dtos;

namespace MicroCMS.Application.Features.Media.Queries;

/// <summary>Returns all media folders for a site, optionally filtered by parent.</summary>
[HasPolicy(ContentPolicies.MediaRead)]
public sealed record ListMediaFoldersQuery(
    Guid SiteId,
    Guid? ParentFolderId = null) : IQuery<IReadOnlyList<MediaFolderDto>>;

/// <summary>Returns a single media folder by ID.</summary>
[HasPolicy(ContentPolicies.MediaRead)]
public sealed record GetMediaFolderQuery(Guid FolderId) : IQuery<MediaFolderDto>;
