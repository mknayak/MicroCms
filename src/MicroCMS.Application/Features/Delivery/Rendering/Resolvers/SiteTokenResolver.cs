using MicroCMS.Application.Common.Interfaces;

namespace MicroCMS.Application.Features.Delivery.Rendering.Resolvers;

/// <summary>
/// Resolves <c>site:*</c> tokens using the existing <see cref="ISettingsReader"/>
/// site-then-tenant resolution chain (with its built-in 5-min cache-aside).
///
/// Supported tokens:
/// <list type="bullet">
///   <item><c>site:name</c> — the site's display name (looked up via <c>site:name</c> key)</item>
///   <item><c>site:settings:YOUR_KEY</c> — arbitrary key from <c>SiteSettings.ConfigEntries</c></item>
/// </list>
///
/// Example: <c>{{site:settings:ga-id}}</c> resolves to the Google Analytics tracking ID
/// stored in site settings under key <c>site:settings:ga-id</c>.
/// </summary>
public sealed class SiteTokenResolver(ISettingsReader settingsReader) : ITokenResolver
{
    public string Namespace => "site";
    public bool Cacheable => true;

    public async Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        IEnumerable<string> tokens,
        RenderContext context,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in tokens)
        {
            // token format: "site:name" or "site:settings:some-key"
            if (!token.StartsWith("site:", StringComparison.OrdinalIgnoreCase))
                continue;

            var remainder = token["site:".Length..];

            // site:settings:* → look up arbitrary key via ISettingsReader
            string settingsKey;
            if (remainder.StartsWith("settings:", StringComparison.OrdinalIgnoreCase))
            {
                settingsKey = token; // full colon-path IS the settings key
            }
            else
            {
                // map well-known tokens to settings keys
                settingsKey = remainder.ToLowerInvariant() switch
                {
                    "name" => "site:name",
                    _      => token // fall through: try the raw key
                };
            }

            var value = await settingsReader.GetAsync(context.SiteId, settingsKey, cancellationToken);
            if (value is not null)
                result[token] = value;
        }

        return result;
    }
}
