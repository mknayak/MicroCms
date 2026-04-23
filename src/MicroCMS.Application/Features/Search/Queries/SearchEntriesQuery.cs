using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Search.Queries;

/// <summary>
/// Full-text / faceted search across published entries for the current tenant (Sprint 9).
/// Tenant scope is enforced server-side; <see cref="ISearchService"/> partitions by tenantId.
/// </summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record SearchEntriesQuery(
    string Query,
    Guid? SiteId = null,
    Guid? ContentTypeId = null,
    string? Locale = null,
    string? Status = "Published",
    int Page = 1,
    int PageSize = 20) : IQuery<SearchResults>;

internal sealed class SearchEntriesQueryHandler(
    ISearchService searchService,
    ICurrentUser currentUser)
  : IRequestHandler<SearchEntriesQuery, Result<SearchResults>>
{
    public async Task<Result<SearchResults>> Handle(
        SearchEntriesQuery request, CancellationToken cancellationToken)
    {
        var req = new SearchRequest(
            request.Query ?? string.Empty,
            request.SiteId,
     request.ContentTypeId,
    request.Locale,
 request.Status,
            request.Page,
      request.PageSize);

        var results = await searchService.SearchAsync(currentUser.TenantId, req, cancellationToken);
        return Result.Success(results);
    }
}
