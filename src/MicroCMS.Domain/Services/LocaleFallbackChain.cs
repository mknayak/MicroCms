using MicroCMS.Domain.ValueObjects;

namespace MicroCMS.Domain.Services;

/// <summary>
/// Domain service that resolves a locale fallback chain for a given requested locale
/// and a tenant's configured enabled locales (FR-CM-8).
///
/// Resolution order example for "en-GB" with default "en-US":
///   1. en-GB (exact match)
///   2. en   (language-only match)
///   3. en-US (tenant default)
/// </summary>
public sealed class LocaleFallbackChain
{
    /// <summary>
    /// Returns an ordered list of locales to attempt, from most specific to least specific.
    /// The caller should use the first locale for which content exists.
    /// </summary>
    public IReadOnlyList<Locale> Build(
        Locale requested,
        Locale tenantDefault,
        IReadOnlyList<Locale> enabledLocales)
    {
        ArgumentNullException.ThrowIfNull(requested, nameof(requested));
        ArgumentNullException.ThrowIfNull(tenantDefault, nameof(tenantDefault));

        var chain = new List<Locale>();

        AddIfEnabled(chain, requested, enabledLocales);
        AddLanguageOnlyFallback(chain, requested, enabledLocales);
        AddIfEnabled(chain, tenantDefault, enabledLocales);

        // Ensure at least one entry
        if (chain.Count == 0)
        {
            chain.Add(tenantDefault);
        }

        return chain.AsReadOnly();
    }

    private static void AddIfEnabled(
        List<Locale> chain,
        Locale locale,
        IReadOnlyList<Locale> enabledLocales)
    {
        if (!chain.Contains(locale) && enabledLocales.Contains(locale))
        {
            chain.Add(locale);
        }
    }

    private static void AddLanguageOnlyFallback(
        List<Locale> chain,
        Locale requested,
        IReadOnlyList<Locale> enabledLocales)
    {
        var parts = requested.Value.Split('-');

        if (parts.Length <= 1)
        {
            return; // already language-only
        }

        var languageOnly = Locale.Create(parts[0]);
        AddIfEnabled(chain, languageOnly, enabledLocales);
    }
}
