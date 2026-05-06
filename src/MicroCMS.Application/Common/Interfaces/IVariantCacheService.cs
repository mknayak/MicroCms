namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Read-through cache for transformed image variants.
/// Implementations store the encoded variant bytes so that a repeated request
/// for the same URL parameters can be served without hitting the database,
/// the storage provider, or the image transform pipeline.
/// </summary>
public interface IVariantCacheService
{
    /// <summary>
    /// Builds a deterministic, filesystem-safe cache key from the request parameters.
    /// </summary>
    string BuildCacheKey(string assetId, int? w, int? h, string fit, string fmt, int q);

    /// <summary>
    /// Returns a cached variant stream, or <c>null</c> if no entry exists for <paramref name="cacheKey"/>.
    /// The caller owns and must dispose the returned stream.
    /// </summary>
    Task<(Stream Content, string MimeType)?> TryGetAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes <paramref name="content"/> into the cache under <paramref name="cacheKey"/>
    /// and returns a new readable stream over the cached bytes (caller owns it).
    /// </summary>
    Task<Stream> SetAsync(string cacheKey, string mimeType, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Removes all cached variants for the given asset <paramref name="assetId"/>.</summary>
    Task InvalidateAsync(string assetId, CancellationToken cancellationToken = default);
}
