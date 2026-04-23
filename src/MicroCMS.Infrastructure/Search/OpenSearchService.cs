using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Shared.Ids;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using AppSearchRequest = MicroCMS.Application.Common.Interfaces.SearchRequest;

namespace MicroCMS.Infrastructure.Search;

/// <summary>
/// Sprint 9 — OpenSearch-backed <see cref="ISearchService"/>.
/// All operations are partitioned by tenant via the alias <c>{IndexPrefix}{tenantId}</c>.
/// Cross-tenant search is prevented by construction: callers pass a <see cref="TenantId"/>
/// which is used to build the alias — never a user-supplied string.
/// </summary>
internal sealed class OpenSearchService : ISearchService
{
    private readonly IOpenSearchClient _client;
    private readonly SearchOptions _options;
 private readonly ILogger<OpenSearchService> _logger;

    public OpenSearchService(
        IOpenSearchClient client,
        IOptions<SearchOptions> options,
     ILogger<OpenSearchService> logger)
    {
    _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task IndexEntryAsync(SearchEntryDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

     var index = IndexFor(document.TenantId);
        var response = await _client.IndexAsync(document, i => i
          .Index(index)
         .Id(document.EntryId.ToString()),
   cancellationToken);

  if (!response.IsValid)
   {
        _logger.LogWarning("OpenSearch index failed for entry {EntryId}: {Error}", document.EntryId, response.DebugInformation);
  }
    }

    public async Task DeleteEntryAsync(TenantId tenantId, Guid entryId, CancellationToken cancellationToken = default)
  {
  var index = IndexFor(tenantId.Value);
        var response = await _client.DeleteAsync<SearchEntryDocument>(
      entryId.ToString(),
          d => d.Index(index),
  cancellationToken);

        if (!response.IsValid && response.ApiCall?.HttpStatusCode != 404)
   {
       _logger.LogWarning("OpenSearch delete failed for entry {EntryId}: {Error}", entryId, response.DebugInformation);
     }
 }

    public async Task<SearchResults> SearchAsync(
TenantId tenantId,
 AppSearchRequest request,
    CancellationToken cancellationToken = default)
 {
     ArgumentNullException.ThrowIfNull(request);

      var from = Math.Max(0, (request.Page - 1) * request.PageSize);
   var index = IndexFor(tenantId.Value);

        var response = await _client.SearchAsync<SearchEntryDocument>(s => s
       .Index(index)
          .From(from)
 .Size(request.PageSize)
    .Query(q => BuildQuery(q, request)),
            cancellationToken);

        if (!response.IsValid)
      {
      _logger.LogWarning("OpenSearch query failed: {Error}", response.DebugInformation);
          return new SearchResults(Array.Empty<SearchHit>(), 0, request.Page, request.PageSize);
        }

     var hits = response.Hits
   .Select(h => new SearchHit(
EntryId: h.Source.EntryId,
      SiteId: h.Source.SiteId,
     ContentTypeId: h.Source.ContentTypeId,
     Slug: h.Source.Slug,
     Locale: h.Source.Locale,
         Status: h.Source.Status,
          Title: h.Source.Title,
         Excerpt: h.Source.Excerpt,
        Score: h.Score ?? 0,
         PublishedAt: h.Source.PublishedAt))
     .ToList();

    return new SearchResults(hits, response.Total, request.Page, request.PageSize);
 }

    private static QueryContainer BuildQuery(QueryContainerDescriptor<SearchEntryDocument> q, AppSearchRequest request)
    {
        var must = new List<Func<QueryContainerDescriptor<SearchEntryDocument>, QueryContainer>>();

       if (!string.IsNullOrWhiteSpace(request.Query))
 {
  must.Add(m => m.MultiMatch(mm => mm
          .Fields(f => f.Field(d => d.Title).Field(d => d.Excerpt).Field(d => d.Body).Field(d => d.Tags))
         .Query(request.Query)
     .Fuzziness(Fuzziness.Auto)));
   }

    if (request.SiteId.HasValue)
  must.Add(m => m.Term(t => t.Field(d => d.SiteId).Value(request.SiteId.Value)));

    if (request.ContentTypeId.HasValue)
     must.Add(m => m.Term(t => t.Field(d => d.ContentTypeId).Value(request.ContentTypeId.Value)));

     if (!string.IsNullOrWhiteSpace(request.Locale))
      must.Add(m => m.Term(t => t.Field(d => d.Locale).Value(request.Locale)));

      if (!string.IsNullOrWhiteSpace(request.Status))
     must.Add(m => m.Term(t => t.Field(d => d.Status).Value(request.Status)));

        return q.Bool(b => b.Must(must.ToArray()));
    }

    private string IndexFor(Guid tenantId) => $"{_options.IndexPrefix}{tenantId:N}";
}
