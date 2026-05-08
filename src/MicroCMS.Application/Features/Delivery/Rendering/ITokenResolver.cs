namespace MicroCMS.Application.Features.Delivery.Rendering;

/// <summary>
/// Resolves all tokens belonging to a single namespace (e.g. <c>page:*</c>, <c>site:*</c>).
///
/// Resolvers are executed sequentially by <see cref="TokenResolutionPipeline"/>.
/// All resolved values are accumulated into a shared dictionary that is reused across
/// subsequent resolvers in the same request, except for the <c>user:*</c> namespace
/// which is always evaluated lazily and never cached.
/// </summary>
public interface ITokenResolver
{
    /// <summary>
    /// Namespace prefix handled by this resolver (without the trailing colon).
    /// Examples: <c>"page"</c>, <c>"template"</c>, <c>"site"</c>, <c>"seo"</c>, <c>"user"</c>.
    /// </summary>
    string Namespace { get; }

    /// <summary>
    /// Whether results from this resolver should be cached in the per-request token dictionary.
    /// Set to <c>false</c> for the <c>user:*</c> resolver.
    /// </summary>
    bool Cacheable { get; }

    /// <summary>
    /// Returns a flat dictionary of <c>token-key → resolved-value</c> for all tokens
    /// found in <paramref name="tokens"/> that belong to this resolver's namespace.
    ///
    /// <paramref name="tokens"/> contains every <c>{{namespace:key}}</c> placeholder
    /// extracted from the shell template.  Implementations should only process tokens
    /// whose prefix matches <see cref="Namespace"/>.
    /// </summary>
    /// <param name="tokens">Distinct token keys (without braces) extracted from the template.</param>
    /// <param name="context">Per-request render data.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>
    /// Dictionary of resolved values.  Keys absent from the return value are treated
    /// as unresolved (the pipeline will emit an HTML comment for them).
    /// </returns>
    Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        IEnumerable<string> tokens,
        RenderContext context,
        CancellationToken cancellationToken = default);
}
