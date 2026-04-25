namespace MicroCMS.Infrastructure.Search;

/// <summary>Sprint 9 — configuration for the full-text search backend.</summary>
public sealed class SearchOptions
{
    public const string SectionName = "Search";

    /// <summary>
    /// Active provider. Allowed values:
    /// <list type="bullet">
    ///   <item><c>Database</c> (default) — SQL <c>LIKE</c> search via EF Core; no external dependencies.</item>
    ///   <item><c>OpenSearch</c> — full-text relevance ranking via an OpenSearch cluster.</item>
    ///   <item><c>None</c> — no-op; all searches return empty results.</item>
    /// </list>
    /// </summary>
    public string Provider { get; set; } = "Database";

    /// <summary>Endpoint URL of the OpenSearch cluster (e.g. <c>http://localhost:9200</c>).</summary>
    public string Endpoint { get; set; } = "http://localhost:9200";

    /// <summary>Optional basic-auth username.</summary>
    public string? Username { get; set; }

    /// <summary>Optional basic-auth password.</summary>
    public string? Password { get; set; }

    /// <summary>
    /// Index alias prefix — the full alias becomes <c>{IndexPrefix}{tenantId}</c> so every
    /// query is partitioned by tenant and cross-tenant queries are impossible.
    /// </summary>
    public string IndexPrefix { get; set; } = "entries-";
}
