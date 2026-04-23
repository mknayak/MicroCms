namespace MicroCMS.Infrastructure.Storage.Signing;

/// <summary>Configuration for the HMAC-based storage signing service.</summary>
public sealed class HmacSigningOptions
{
    public const string SectionName = "Storage:Signing";

    /// <summary>Secret key used to sign URLs. Must be at least 32 characters long.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional base URL override (e.g. <c>https://api.example.com</c>).
    /// Inferred from the current HTTP request when not set.
    /// </summary>
    public string? BaseUrl { get; set; }
}
