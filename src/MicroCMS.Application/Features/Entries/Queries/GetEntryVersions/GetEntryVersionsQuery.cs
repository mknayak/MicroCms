using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.Application.Features.Entries.Queries.GetEntryVersions;

/// <summary>Returns all historical versions for a given entry, ordered newest first.</summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record GetEntryVersionsQuery(Guid EntryId) : IQuery<IReadOnlyList<EntryVersionDto>>;
