using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Full-text and faceted search over published entries (Sprint 9).
/// Implementations are tenant-scoped — every call is partitioned by <paramref name="tenantId"/>.
/// The default OpenSearch adapter stores documents in the alias
/// <c>entries-{tenantId}</c>; cross-tenant queries are blocked at the gateway.
/// </summary>
public interface ISearchService
{
    /// <summary>Indexes or updates a single document.</summary>
    Task IndexEntryAsync(SearchEntryDocument document, CancellationToken cancellationToken = default);

    /// <summary>Removes an entry from the search index.</summary>
    Task DeleteEntryAsync(TenantId tenantId, Guid entryId, CancellationToken cancellationToken = default);

    /// <summary>Executes a tenant-scoped search.</summary>
    Task<SearchResults> SearchAsync(
        TenantId tenantId,
        SearchRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Document shape pushed to the search index.</summary>
public sealed record SearchEntryDocument(
    Guid EntryId,
    Guid TenantId,
    Guid SiteId,
    Guid ContentTypeId,
    string Slug,
    string Locale,
    string Status,
    string? Title,
  string? Excerpt,
  string? Body,
    IReadOnlyList<string> Tags,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);

/// <summary>Search query parameters.</summary>
public sealed record SearchRequest(
    string Query,
    Guid? SiteId = null,
    Guid? ContentTypeId = null,
    string? Locale = null,
    string? Status = "Published",
    int Page = 1,
    int PageSize = 20);

/// <summary>Search result hit.</summary>
public sealed record SearchHit(
    Guid EntryId,
    Guid SiteId,
    Guid ContentTypeId,
 string Slug,
    string Locale,
    string Status,
    string? Title,
    string? Excerpt,
    double Score,
    DateTimeOffset? PublishedAt);

/// <summary>Search response envelope.</summary>
public sealed record SearchResults(
    IReadOnlyList<SearchHit> Hits,
    long TotalCount,
    int Page,
    int PageSize);
