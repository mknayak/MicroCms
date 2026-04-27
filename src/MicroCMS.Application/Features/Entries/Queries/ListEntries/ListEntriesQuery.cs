using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.Entries.Queries.ListEntries;

/// <summary>
/// Returns a paginated list of entries for a site, with optional filters
/// for status, content type, locale, and folder.
/// </summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record ListEntriesQuery(
    Guid SiteId,
    string? StatusFilter = null,
    Guid? ContentTypeId = null,
    string? Locale = null,
    Guid? FolderId = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedList<EntryListItemDto>>;
