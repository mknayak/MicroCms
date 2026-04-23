namespace MicroCMS.Infrastructure.Caching;

/// <summary>Sprint 9 — configuration for the two-tier cache.</summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

  /// <summary>
    /// Active L2 provider: <c>Redis</c> or <c>None</c>. Defaults to <c>None</c> which keeps
/// only the in-process L1 memory cache — safe for development and single-instance deployments.
    /// </summary>
    public string Provider { get; set; } = "None";

    /// <summary>StackExchange.Redis connection string. Required when <see cref="Provider"/> = Redis.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Default absolute expiry when callers do not supply one. Defaults to 10 minutes.</summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Key prefix applied to every L2 entry — prevents collisions in shared Redis clusters.</summary>
    public string KeyPrefix { get; set; } = "microcms:";
}
