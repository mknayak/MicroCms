using System.Text.RegularExpressions;

namespace MicroCMS.Delivery.Core.Rendering;

/// <summary>
/// Utilities for Scriban template preprocessing.
/// <para>
/// The token resolution pipeline uses <c>{{ns:key}}</c> syntax (e.g. <c>{{zone:hero}}</c>,
/// <c>{{seo:title}}</c>, <c>{{page:slug}}</c>). Because the colon makes these invalid
/// Scriban expressions, they must be stashed as opaque placeholders before the template
/// is parsed, then restored in the rendered output so the downstream pipeline can resolve them.
/// </para>
/// </summary>
internal static class ScribanHelpers
{
    // Matches {{ns:key}} tokens — any token that contains at least one colon segment.
    private static readonly Regex NamespaceTokenPattern = new(
        @"\{\{([a-zA-Z][a-zA-Z0-9_-]*(?::[a-zA-Z0-9_\-\.]+)+)\}\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Replaces every <c>{{ns:key}}</c> token in <paramref name="template"/> with a
    /// unique opaque placeholder. Returns the modified template and the stash dictionary
    /// needed by <see cref="Restore"/>.
    /// </summary>
    internal static string Stash(string template, out Dictionary<string, string> stash)
    {
        var localStash = new Dictionary<string, string>(StringComparer.Ordinal);
        int idx = 0;
        var result = NamespaceTokenPattern.Replace(template, m =>
        {
            var placeholder = $"__ns_{idx++}__";
            localStash[placeholder] = m.Value;
            return placeholder;
        });
        stash = localStash;
        return result;
    }

    /// <summary>
    /// Replaces all placeholders in <paramref name="rendered"/> with their original
    /// <c>{{ns:key}}</c> tokens so the token resolution pipeline can process them.
    /// </summary>
    internal static string Restore(string rendered, Dictionary<string, string> stash)
    {
        foreach (var (placeholder, original) in stash)
            rendered = rendered.Replace(placeholder, original, StringComparison.Ordinal);
        return rendered;
    }
}
