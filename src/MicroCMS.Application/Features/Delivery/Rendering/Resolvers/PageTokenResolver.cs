namespace MicroCMS.Application.Features.Delivery.Rendering.Resolvers;

/// <summary>
/// Resolves <c>page:*</c> tokens from the current <see cref="RenderContext"/>.
///
/// Built-in tokens:
/// <list type="bullet">
///   <item><c>page:slug</c> — URL slug of the page</item>
///   <item><c>page:title</c> — display title</item>
///   <item><c>page:published-at</c> — ISO-8601 publish timestamp, or empty if not published</item>
/// </list>
///
/// Dynamic tokens (from the page's linked entry):
/// Any <c>page:{fieldName}</c> token whose key does not match a built-in is looked up
/// in <see cref="RenderContext.PageFields"/>, which is populated from the linked entry's
/// field JSON. For example, if the entry has a field named <c>author</c>, the token
/// <c>{{page:author}}</c> will be resolved to that field's value.
/// </summary>
public sealed class PageTokenResolver : ITokenResolver
{
    public string Namespace => "page";
    public bool Cacheable => true;

    public Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        IEnumerable<string> tokens,
        RenderContext context,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in tokens)
            ResolveToken(token, context, result);

        return Task.FromResult<IReadOnlyDictionary<string, string>>(result);
    }

    private static void ResolveToken(
        string token,
        RenderContext context,
        Dictionary<string, string> result)
    {
        if (TryResolveBuiltIn(token, context, out var builtIn))
        {
            result[token] = builtIn!;
            return;
        }

        // Dynamic field tokens (page:fieldName) — look up in PageFields.
        var fieldName = token.Length > 5 ? token[5..] : string.Empty;
        if (!string.IsNullOrEmpty(fieldName)
            && context.PageFields.TryGetValue(fieldName, out var fieldValue))
        {
            result[token] = fieldValue;
        }
    }

    private static bool TryResolveBuiltIn(string token, RenderContext context, out string? value)
    {
        value = token.ToLowerInvariant() switch
        {
            "page:slug"         => context.PageSlug,
            "page:pagetitle"        => context.PageTitle,
            "page:published-at" => context.PagePublishedAt?.ToString("O") ?? string.Empty,
            _                   => null
        };
        return value is not null;
    }
}
