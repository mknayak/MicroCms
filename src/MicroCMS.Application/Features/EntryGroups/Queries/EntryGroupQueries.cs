using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.EntryGroups.Dtos;

namespace MicroCMS.Application.Features.EntryGroups.Queries;

/// <summary>Returns all groups for a content type on the current site.</summary>
[HasPolicy(ContentPolicies.EntryGroupRead)]
public sealed record ListEntryGroupsQuery(Guid ContentTypeId) : IQuery<IReadOnlyList<EntryGroupListItemDto>>;

/// <summary>Returns a single group with its full member list.</summary>
[HasPolicy(ContentPolicies.EntryGroupRead)]
public sealed record GetEntryGroupQuery(Guid GroupId) : IQuery<EntryGroupDto>;
