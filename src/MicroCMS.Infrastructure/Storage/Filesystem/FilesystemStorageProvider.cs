using MicroCMS.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace MicroCMS.Infrastructure.Storage.Filesystem;

/// <summary>
/// Stores uploaded files under a configured root directory on the local filesystem.
/// Storage key format: <c>{tenantId}/{year}/{month}/{guid}_{sanitisedFileName}</c>.
/// Suitable for single-node development / on-premises deployments.
/// </summary>
public sealed class FilesystemStorageProvider : IStorageProvider
{
    private readonly FilesystemStorageOptions _options;

    public FilesystemStorageProvider(IOptions<FilesystemStorageOptions> options) =>
        _options = options.Value;

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string mimeType,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(tenantId, fileName);
        var fullPath = ToAbsolutePath(key);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81_920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
        return key;
    }

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ToAbsolutePath(storageKey);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81_920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ToAbsolutePath(storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<string> GetPublicUrlAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        // Filesystem provider does not expose a public URL by default;
        // callers must use the signed-URL endpoint instead.
        return Task.FromResult(string.Empty);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(ToAbsolutePath(storageKey)));

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string BuildKey(string tenantId, string fileName)
    {
        var now = DateTimeOffset.UtcNow;
        var safeName = SanitiseFileName(fileName);
        return Path.Combine(tenantId, now.Year.ToString(), now.Month.ToString("D2"),
            $"{Guid.NewGuid():N}_{safeName}");
    }

    private string ToAbsolutePath(string storageKey) =>
        Path.GetFullPath(Path.Combine(_options.RootPath, storageKey));

    private static string SanitiseFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat(fileName.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));
        return safe.Length > 100 ? safe[^100..] : safe;
    }
}
