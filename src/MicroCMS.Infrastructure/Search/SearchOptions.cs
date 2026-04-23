namespace MicroCMS.Infrastructure.Search;

/// <summary>Sprint 9 — configuration for the full-text search backend.</summary>
public sealed class SearchOptions
{
    public const string SectionName = "Search";

    /// <summary>Active provider: <c>OpenSearch</c> or <c>None</c>. Defaults to <c>None</c>.</summary>
    public string Provider { get; set; } = "None";

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
