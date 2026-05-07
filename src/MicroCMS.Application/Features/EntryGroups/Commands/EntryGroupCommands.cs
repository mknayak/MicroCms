using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.EntryGroups.Dtos;

namespace MicroCMS.Application.Features.EntryGroups.Commands;

/// <summary>Creates a new entry group for the given content type.</summary>
[HasPolicy(ContentPolicies.EntryGroupManage)]
public sealed record CreateEntryGroupCommand(
    Guid ContentTypeId,
    string Handle,
    string Title,
    string? Description = null,
    Guid? ImageAssetId = null,
    IReadOnlyList<Guid>? MemberEntryIds = null) : ICommand<EntryGroupDto>;

/// <summary>Updates title, description, image, and/or member list of an existing group.</summary>
[HasPolicy(ContentPolicies.EntryGroupManage)]
public sealed record UpdateEntryGroupCommand(
    Guid GroupId,
    string Title,
    string? Description = null,
    Guid? ImageAssetId = null,
    IReadOnlyList<Guid>? MemberEntryIds = null) : ICommand<EntryGroupDto>;

/// <summary>Deletes an entry group. Does not delete the member entries themselves.</summary>
[HasPolicy(ContentPolicies.EntryGroupManage)]
public sealed record DeleteEntryGroupCommand(Guid GroupId) : ICommand;
