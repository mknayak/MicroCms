using System.Collections.ObjectModel;

namespace MicroCMS.SiteHost;

/// <summary>
/// Root configuration section: <c>SiteHost</c>.
/// </summary>
public sealed class SiteHostOptions
{
    public const string Section = "SiteHost";

    /// <summary>Base URL of MicroCMS.Delivery.WebHost — single source of truth.</summary>
    public Uri DeliveryBaseUrl { get; init; } = new("http://localhost:5200");

    /// <summary>
    /// Fallback site used when no entry in <see cref="Sites"/> matches the incoming hostname.
    /// <c>null</c> means no fallback — unknown hostnames receive 421 Misdirected Request.
    /// </summary>
    public SiteEntry? DefaultSite { get; init; }

    /// <summary>One entry per live hostname.</summary>
    public Collection<SiteEntry> Sites { get; init; } = [];
}

/// <summary>
/// Maps a public hostname to its MicroCMS site identity.
/// </summary>
public sealed class SiteEntry
{
    /// <summary>
    /// The <c>Host</c> header value as seen on incoming requests,
    /// e.g. <c>site-a.com</c> or <c>localhost</c>.
    /// Port is stripped before matching, so <c>localhost:5300</c> matches <c>localhost</c>.
    /// </summary>
    public string Hostname { get; init; } = string.Empty;

    /// <summary>MicroCMS SiteId Guid for this hostname.</summary>
    public string SiteId { get; init; } = string.Empty;

    /// <summary>X-Api-Key issued from Admin → Sites → API Clients.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>BCP-47 locale forwarded to the delivery render endpoint.</summary>
    public string DefaultLocale { get; init; } = "en";
}
