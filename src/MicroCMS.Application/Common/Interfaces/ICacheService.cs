namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the two-tier cache (L1 in-memory + L2 Redis).
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Stores a value under <paramref name="key"/> and registers it under <paramref name="tag"/>
    /// for bulk invalidation via <see cref="RemoveByTagAsync"/>.
    /// </summary>
    Task SetWithTagAsync<T>(
        string key,
        T value,
        string tag,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
