namespace MicroCMS.Domain.Services;

/// <summary>
/// Generates time-limited signed URLs for private media assets (GAP-16).
/// Implementations live in Infrastructure and are bound to the configured storage provider
/// (local HMAC, S3 pre-signed, Azure SAS, etc.).
/// </summary>
public interface IStorageSigningService
{
    /// <summary>
    /// Returns a signed delivery URL for <paramref name="storageKey"/> that expires after
    /// <paramref name="expiresIn"/>. The URL is opaque to the caller; never stored in the DB.
    /// </summary>
    Task<string> GenerateSignedUrlAsync(
   string storageKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);
}
