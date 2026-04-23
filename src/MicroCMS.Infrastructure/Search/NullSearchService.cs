using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Infrastructure.Search;

/// <summary>
/// Sprint 9 — no-op search service used when no search backend is configured.
/// Indexing calls silently succeed; searches return an empty result set.
/// Keeps the application bootable without OpenSearch for local dev and tests.
/// </summary>
internal sealed class NullSearchService : ISearchService
{
    public Task IndexEntryAsync(SearchEntryDocument document, CancellationToken cancellationToken = default)
   => Task.CompletedTask;

    public Task DeleteEntryAsync(TenantId tenantId, Guid entryId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<SearchResults> SearchAsync(TenantId tenantId, SearchRequest request, CancellationToken cancellationToken = default)
 => Task.FromResult(new SearchResults(
        Hits: Array.Empty<SearchHit>(),
      TotalCount: 0,
Page: request.Page,
       PageSize: request.PageSize));
}
