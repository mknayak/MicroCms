using System.Security.Cryptography;
using System.Text;
using MicroCMS.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MicroCMS.Infrastructure.Storage.Database;

namespace MicroCMS.Infrastructure.Storage.Imaging;

/// <summary>
/// Filesystem-backed variant cache.
///
/// Layout under <c>CacheRoot/variants/</c>:
/// <code>
///   {CacheRoot}/variants/{sha256(cacheKey)}.{ext}
///   {CacheRoot}/variants/{sha256(cacheKey)}.mime
/// </code>
/// The <c>.mime</c> sidecar stores the MIME type so the controller can set
/// <c>Content-Type</c> without re-running the transform pipeline.
///
/// Invalidation removes all <c>{sha256}.*</c> files whose key starts with the
/// given storage key prefix — achieved by storing a reverse-index file.
/// </summary>
public sealed class FilesystemVariantCacheService(
    IOptions<DatabaseStorageOptions> options,
    IHostEnvironment env) : IVariantCacheService
{
    private string CacheRoot
    {
        get
        {
            var path = options.Value.CachePath;
            var root = Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(env.ContentRootPath, path));
            return Path.Combine(root, "variants");
        }
    }

    public async Task<(Stream Content, string MimeType)?> TryGetAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        var (dataPath, mimePath) = Paths(cacheKey);

        if (!File.Exists(dataPath) || !File.Exists(mimePath))
            return null;

        var mimeType = await File.ReadAllTextAsync(mimePath, cancellationToken);
        var stream = new FileStream(dataPath, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 81_920, useAsync: true);
        return (stream, mimeType);
    }

    public async Task<Stream> SetAsync(
        string cacheKey,
        string mimeType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var (dataPath, mimePath) = Paths(cacheKey);
        Directory.CreateDirectory(Path.GetDirectoryName(dataPath)!);

        // Buffer into MemoryStream first so we can both write to disk and return a
        // readable stream without seeking the original (which may not be seekable).
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);

        await File.WriteAllBytesAsync(dataPath, ms.ToArray(), cancellationToken);
        await File.WriteAllTextAsync(mimePath, mimeType, cancellationToken);

        ms.Position = 0;
        return new MemoryStream(ms.ToArray(), writable: false);
    }

    public Task InvalidateAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var root = CacheRoot;
        if (!Directory.Exists(root))
            return Task.CompletedTask;

        // Variant cache keys are built as "{storageKey}|..." so any file whose
        // base name hashes to a key starting with this storage key cannot be found
        // by hash alone. Instead we keep a plain-text index file per storage key.
        var indexPath = IndexPath(storageKey);
        if (!File.Exists(indexPath))
            return Task.CompletedTask;

        foreach (var hashedKey in File.ReadAllLines(indexPath))
        {
            var stem = Path.Combine(root, hashedKey);
            File.Delete(stem + ".bin");
            File.Delete(stem + ".mime");
        }

        File.Delete(indexPath);
        return Task.CompletedTask;
    }

    // ── IVariantCacheService ───────────────────────────────────────────────

    public string BuildCacheKey(string assetId, int? w, int? h, string fit, string fmt, int q)
    {
        var canonical = $"{assetId}|w={w}&h={h}&fit={fit}&fmt={fmt}&q={q}";
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();

        // Update reverse index so InvalidateAsync can find this entry later.
        var indexPath = IndexPath(assetId);
        Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
        File.AppendAllLines(indexPath, [hash]);

        return hash;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private (string dataPath, string mimePath) Paths(string cacheKey)
    {
        var root = CacheRoot;
        return (Path.Combine(root, cacheKey + ".bin"),
                Path.Combine(root, cacheKey + ".mime"));
    }

    private string IndexPath(string storageKey)
    {
        // Use the same SHA-256 approach for the index file name to keep the
        // filename filesystem-safe (storage keys can contain path separators).
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(storageKey))).ToLowerInvariant();
        return Path.Combine(CacheRoot, "idx_" + hash + ".idx");
    }
}
