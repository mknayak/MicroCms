using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MicroCMS.Infrastructure.Storage.Database;

/// <summary>
/// Stores binary assets as BLOBs in the application database.
///
/// Read-through filesystem cache:
/// On <see cref="DownloadAsync"/>, files at or below <see cref="DatabaseStorageOptions.MaxCachedFileSizeBytes"/>
/// are written to <see cref="DatabaseStorageOptions.CachePath"/> after the first DB read.
/// Subsequent reads are served from disk, avoiding repeated DB round-trips.
///
/// <see cref="DatabaseStorageOptions.CachePath"/> is resolved relative to the application's
/// content root (e.g. <c>App_Data\media-cache</c>) so the process never writes outside its
/// own directory tree.
///
/// Trade-offs vs. object storage:
/// + Zero external dependencies — works with SQLite, PostgreSQL, SQL Server.
/// + Web-farm safe: every node shares the same DB row.
/// – Not suitable for very large files or high read throughput; prefer S3/AzureBlob for production scale.
/// </summary>
public sealed class DatabaseStorageProvider(
    ApplicationDbContext db,
    IOptions<DatabaseStorageOptions> options,
    IHostEnvironment env) : IStorageProvider
{
    private readonly DatabaseStorageOptions _options = options.Value;

    /// <summary>
    /// Absolute root of the cache directory, resolved once from the content root.
    /// Relative paths are anchored to <see cref="IHostEnvironment.ContentRootPath"/>;
    /// absolute paths are used as-is (allows override in integration tests).
    /// </summary>
    private string CacheRoot => Path.IsPathRooted(_options.CachePath)
        ? _options.CachePath
        : Path.GetFullPath(Path.Combine(env.ContentRootPath, _options.CachePath));

    // ── IStorageProvider ──────────────────────────────────────────────────

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string mimeType,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(tenantId, fileName);

        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        var data = ms.ToArray();

        var record = MediaBlobRecord.Create(key, mimeType, data);
        db.MediaBlobs.Add(record);
        await db.SaveChangesAsync(cancellationToken);

        return key;
    }

    public async Task<Stream> DownloadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var cachePath = CacheFilePath(storageKey);

        if (File.Exists(cachePath))
            return new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81_920, useAsync: true);

        var record = await db.MediaBlobs
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.StorageKey == storageKey, cancellationToken)
            ?? throw new FileNotFoundException($"Storage key not found: {storageKey}");

        // Populate cache for files within the size threshold
        if (record.Data.Length <= _options.MaxCachedFileSizeBytes)
        {
            var dir = Path.GetDirectoryName(cachePath)!;
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(cachePath, record.Data, cancellationToken);
            return new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81_920, useAsync: true);
        }

        // File too large to cache — stream directly from the byte array
        return new MemoryStream(record.Data, writable: false);
    }

    public async Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.MediaBlobs
            .Where(b => b.StorageKey == storageKey)
            .ExecuteDeleteAsync(cancellationToken);

        if (rows > 0)
            InvalidateCache(storageKey);
    }

    public Task<string> GetPublicUrlAsync(
        string storageKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(string.Empty); // No direct public URL; use the signed-URL endpoint.

    public async Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default) =>
        File.Exists(CacheFilePath(storageKey)) ||
        await db.MediaBlobs.AnyAsync(b => b.StorageKey == storageKey, cancellationToken);

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string BuildKey(string tenantId, string fileName)
    {
        var now = DateTimeOffset.UtcNow;
        var safeName = SanitiseFileName(fileName);
        return $"{tenantId}/{now.Year}/{now.Month:D2}/{Guid.NewGuid():N}_{safeName}";
    }

    private string CacheFilePath(string storageKey) =>
        Path.GetFullPath(Path.Combine(CacheRoot, storageKey));

    private void InvalidateCache(string storageKey)
    {
        var path = CacheFilePath(storageKey);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static string SanitiseFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat(fileName.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));
        return safe.Length > 100 ? safe[^100..] : safe;
    }
}
