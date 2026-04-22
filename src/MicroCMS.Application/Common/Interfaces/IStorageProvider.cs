namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Abstraction over binary object storage (Filesystem, S3, Azure Blob, GCS, etc.).
/// All keys are provider-neutral strings; callers never interpret the format.
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// Streams <paramref name="content"/> to the provider and returns the opaque storage key
    /// that can later be used with <see cref="DownloadAsync"/> or <see cref="DeleteAsync"/>.
    /// </summary>
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string mimeType,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Opens a read-only stream for the object identified by <paramref name="storageKey"/>.</summary>
    Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>Permanently removes the object. No-ops if the key does not exist.</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a public (unauthenticated) delivery URL, or an empty string if the provider
    /// does not support public access (e.g. private-only Filesystem provider).
    /// </summary>
    Task<string> GetPublicUrlAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>Returns true when an object with the given key exists in storage.</summary>
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);
}
