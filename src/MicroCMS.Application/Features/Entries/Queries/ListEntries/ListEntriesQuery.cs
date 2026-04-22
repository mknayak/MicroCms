using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.Entries.Queries.ListEntries;

/// <summary>
/// Returns a paginated list of entries for a site, with optional status filter.
/// </summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record ListEntriesQuery(
    Guid SiteId,
    string? StatusFilter = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedList<EntryListItemDto>>;
