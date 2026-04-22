using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// Represents a single deployment environment attached to a site (GAP-17).
/// e.g. Production at https://example.com, Staging at https://staging.example.com.
/// </summary>
public sealed class SiteEnvironment : ValueObject
{
    private SiteEnvironment() { } // EF Core

    private SiteEnvironment(EnvironmentType type, string url, bool isLive)
    {
        Type = type;
    Url = url;
        IsLive = isLive;
 SslStatus = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
          ? SslStatus.Valid
      : SslStatus.None;
    }

    public EnvironmentType Type { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public SslStatus SslStatus { get; private set; }
    public bool IsLive { get; private set; }

    public static SiteEnvironment Create(EnvironmentType type, string url, bool isLive = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));
      if (!Uri.TryCreate(url, UriKind.Absolute, out _))
    throw new DomainException($"SiteEnvironment URL '{url}' is not a valid absolute URL.");
        return new SiteEnvironment(type, url.TrimEnd('/'), isLive);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Url;
 }
}

/// <summary>SSL certificate status for a site environment.</summary>
public enum SslStatus { None = 0, Valid = 1, Expiring = 2, Expired = 3 }
