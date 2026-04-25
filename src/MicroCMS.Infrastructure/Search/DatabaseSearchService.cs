using System.Text.Json;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Enums;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;

namespace MicroCMS.Infrastructure.Search;

/// <summary>
/// DB-backed <see cref="ISearchService"/> that queries <see cref="ApplicationDbContext"/>
/// directly using <see cref="EF.Functions.Like"/> on <c>FieldsJson</c> and <c>Slug</c>.
///
/// This is the default provider (<c>Search:Provider = Database</c>) — it works out-of-the-box
/// without any external search infrastructure and is a good fit for small-to-medium tenants.
/// Switch to <c>OpenSearch</c> for full-text relevance ranking and faceted search at scale.
///
/// Security: tenant isolation is enforced by the <see cref="ApplicationDbContext"/> global
/// query filter — the <c>tenantId</c> parameter is an additional safety guard.
/// Indexing operations are no-ops: the database is the source of truth.
/// </summary>
internal sealed class DatabaseSearchService : ISearchService
{
    private const int ExcerptMaxLength = 200;

    private readonly ApplicationDbContext _db;

    public DatabaseSearchService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ── ISearchService ────────────────────────────────────────────────────

    /// <summary>
    /// No-op — the entry is already in the DB; no secondary index to maintain.
    /// </summary>
    public Task IndexEntryAsync(
        SearchEntryDocument document,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// No-op — deletion from the DB is handled by the repository layer.
    /// </summary>
    public Task DeleteEntryAsync(
        TenantId tenantId,
        Guid entryId,
        CancellationToken cancellationToken = default)
   => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task<SearchResults> SearchAsync(
        TenantId tenantId,
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The DbContext global query filter already scopes to _tenantFilter, but we add
        // an explicit guard here so that callers bypassing the filter (e.g. background jobs)
        // still receive only the requested tenant's data.
        var query = _db.Entries
.AsNoTracking()
            .Where(e => e.TenantId == tenantId);

    query = ApplyFilters(query, request);

        var totalCount = await query.LongCountAsync(cancellationToken);

      var skip = Math.Max(0, (request.Page - 1) * request.PageSize);
    var entries = await query
    .OrderByDescending(e => e.UpdatedAt)
  .Skip(skip)
  .Take(request.PageSize)
          .ToListAsync(cancellationToken);

        var hits = entries.Select(e =>
        {
      var (title, excerpt) = ExtractTitleExcerpt(e.FieldsJson);
            return new SearchHit(
EntryId: e.Id.Value,
      SiteId: e.SiteId.Value,
         ContentTypeId: e.ContentTypeId.Value,
       Slug: e.Slug.Value,
           Locale: e.Locale.Value,
     Status: e.Status.ToString(),
    Title: title,
                Excerpt: excerpt,
                Score: 1.0,          // DB LIKE has no relevance score
    PublishedAt: e.PublishedAt);
        }).ToList();

        return new SearchResults(hits, totalCount, request.Page, request.PageSize);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static IQueryable<Domain.Aggregates.Content.Entry> ApplyFilters(
     IQueryable<Domain.Aggregates.Content.Entry> query,
    SearchRequest request)
    {
        if (request.SiteId.HasValue)
            query = query.Where(e => e.SiteId == new SiteId(request.SiteId.Value));

        if (request.ContentTypeId.HasValue)
            query = query.Where(e => e.ContentTypeId == new ContentTypeId(request.ContentTypeId.Value));

        if (!string.IsNullOrWhiteSpace(request.Locale))
    {
   var locale = request.Locale;
            query = query.Where(e => e.Locale.Value == locale);
        }

if (!string.IsNullOrWhiteSpace(request.Status) &&
  Enum.TryParse<EntryStatus>(request.Status, ignoreCase: true, out var status))
     {
            query = query.Where(e => e.Status == status);
        }

    if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var pattern = $"%{request.Query}%";
            query = query.Where(e =>
   EF.Functions.Like(e.FieldsJson, pattern) ||
     EF.Functions.Like(e.Slug.Value, pattern));
        }

      return query;
    }

    private static (string? title, string? excerpt) ExtractTitleExcerpt(string fieldsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(fieldsJson);
            var root = doc.RootElement;
    var title   = TryGetString(root, "title");
     var excerpt = TryGetString(root, "excerpt")
 ?? Truncate(TryGetString(root, "body"), ExcerptMaxLength);
            return (title, excerpt);
        }
        catch (JsonException)
        {
          return (null, null);
        }
    }

    private static string? TryGetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
  : null;

    private static string? Truncate(string? text, int maxLength) =>
        text is null ? null
        : text.Length <= maxLength ? text
        : string.Concat(text.AsSpan(0, maxLength), "…");
}
