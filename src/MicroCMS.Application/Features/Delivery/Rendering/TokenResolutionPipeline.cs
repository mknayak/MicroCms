using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MicroCMS.Application.Features.Delivery.Rendering;

/// <summary>
/// Orchestrates sequential token resolution over a shell template string.
///
/// Algorithm:
/// 1. Extract all <c>{{namespace:key}}</c> placeholders from the template.
/// 2. For each registered <see cref="ITokenResolver"/> (ordered by registration):
///    a. Filter tokens belonging to the resolver's namespace.
///    b. If cacheable and all its tokens are already resolved, skip.
///    c. Otherwise call <see cref="ITokenResolver.ResolveAsync"/> and merge results.
/// 3. Substitute all resolved tokens in the template.
/// 4. Remaining unresolved tokens → replaced by an HTML comment fallback.
/// 5. <c>user:*</c> tokens in a shell-level template → <see cref="LogLevel.Warning"/> + HTML comment.
/// </summary>
public sealed class TokenResolutionPipeline(
    IEnumerable<ITokenResolver> resolvers,
    ILogger<TokenResolutionPipeline> logger)
{
    // Matches {{namespace:key}} — colon-delimited, no whitespace.
    private static readonly Regex TokenPattern = new(
        @"\{\{([a-zA-Z][a-zA-Z0-9_-]*(?::[a-zA-Z0-9_\-\.]+)+)\}\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IReadOnlyList<ITokenResolver> _resolvers = resolvers.ToList();

    /// <summary>
    /// Resolves all tokens in <paramref name="template"/> and returns the substituted HTML string.
    /// </summary>
    public async Task<string> ResolveAsync(
        string template,
        RenderContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        var allTokens = ExtractTokens(template);
        if (allTokens.Count == 0)
            return template;

        WarnUserNamespaceTokens(allTokens);

        var resolved = await RunResolversAsync(allTokens, context, cancellationToken);

        return Substitute(template, resolved);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static List<string> ExtractTokens(string template) =>
        TokenPattern
            .Matches(template)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private void WarnUserNamespaceTokens(IEnumerable<string> tokens)
    {
        foreach (var token in tokens.Where(t => t.StartsWith("user:", StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogWarning(
                "Token '{Token}' belongs to the user:* namespace which is not safe in shell templates. " +
                "It will not be resolved. Move it to a component scope if user-contextual rendering is required.",
                token);
        }
    }

    private async Task<Dictionary<string, string>> RunResolversAsync(
        List<string> allTokens,
        RenderContext context,
        CancellationToken cancellationToken)
    {
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resolver in _resolvers)
        {
            var prefix = resolver.Namespace + ":";
            var namespaceTokens = allTokens
                .Where(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (namespaceTokens.Count == 0)
                continue;

            if (resolver.Cacheable && namespaceTokens.All(resolved.ContainsKey))
                continue;

            await InvokeResolverAsync(resolver, namespaceTokens, context, resolved, cancellationToken);
        }

        return resolved;
    }

    private async Task InvokeResolverAsync(
        ITokenResolver resolver,
        List<string> tokens,
        RenderContext context,
        Dictionary<string, string> resolved,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> results;
        try
        {
            results = await resolver.ResolveAsync(tokens, context, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token resolver for namespace '{Namespace}' threw an exception.", resolver.Namespace);
            return;
        }

        foreach (var (key, value) in results)
            resolved[key] = value;
    }

    private string Substitute(string template, Dictionary<string, string> resolved) =>
        TokenPattern.Replace(template, match =>
        {
            var token = match.Groups[1].Value;

            if (token.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
                return $"<!-- token not resolved: {token} (user:* not available in shell context) -->";

            if (resolved.TryGetValue(token, out var value))
                return value;

            logger.LogDebug("Token '{Token}' was not resolved by any resolver.", token);
            return $"<!-- token not found: {token} -->";
        });
}
