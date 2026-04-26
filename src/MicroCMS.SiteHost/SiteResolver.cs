using Microsoft.Extensions.Options;

namespace MicroCMS.SiteHost;

/// <summary>
/// Resolves the <see cref="SiteEntry"/> for the current request.
///
/// Resolution order:
///   1. Exact hostname match (case-insensitive, port stripped) from <see cref="SiteHostOptions.Sites"/>.
///   2. <see cref="SiteHostOptions.DefaultSite"/> if configured.
///   3. <c>null</c> — caller should return 421 Misdirected Request.
/// </summary>
public sealed class SiteResolver(IOptions<SiteHostOptions> options)
{
    private readonly IReadOnlyList<SiteEntry> _sites = options.Value.Sites;
    private readonly SiteEntry? _defaultSite = options.Value.DefaultSite;

    /// <summary>
    /// Returns the best matching <see cref="SiteEntry"/>, or <c>null</c> if no match
    /// and no default is configured.
    /// </summary>
    public SiteEntry? Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Strip port: "site-a.com:443" → "site-a.com"
        var host = context.Request.Host.Host;

        // 1. Exact hostname match
        var match = _sites.FirstOrDefault(s =>
            string.Equals(s.Hostname, host, StringComparison.OrdinalIgnoreCase));

        // 2. Fall back to DefaultSite (may itself be null → 421 returned by caller)
        return match ?? _defaultSite;
    }
}
