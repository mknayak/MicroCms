namespace MicroCMS.Domain.Services;

/// <summary>
/// Generates and validates time-limited signed URLs for private media assets (GAP-16).
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

 /// <summary>
    /// Validates a signed URL's HMAC and expiry.
    /// Returns <c>true</c> when the signature is valid and the URL has not expired.
    /// </summary>
    bool Validate(string storageKey, long expiresAt, string tenantId, string signature);
}
