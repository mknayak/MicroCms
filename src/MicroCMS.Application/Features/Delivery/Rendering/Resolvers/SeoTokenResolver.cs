namespace MicroCMS.Application.Features.Delivery.Rendering.Resolvers;

/// <summary>
/// Resolves legacy <c>seo:*</c> tokens that were already supported by the renderer
/// before the token pipeline was introduced.
///
/// Supported tokens:
/// <list type="bullet">
///   <item><c>seo:title</c></item>
///   <item><c>seo:description</c></item>
///   <item><c>seo:ogImage</c></item>
/// </list>
///
/// Values come from the <see cref="RenderContext"/> which is populated by
/// <c>RenderPageBySlugQueryHandler.ResolveSeoAsync</c>.
/// </summary>
public sealed class SeoTokenResolver : ITokenResolver
{
    public string Namespace => "seo";
    public bool Cacheable => true;

    /// <summary>Injected by the query handler before resolution starts.</summary>
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoOgImage { get; set; }

    public Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        IEnumerable<string> tokens,
        RenderContext context,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in tokens)
        {
            var value = token.ToLowerInvariant() switch
            {
                "seo:title"       => SeoTitle       ?? string.Empty,
                "seo:description" => SeoDescription ?? string.Empty,
                "seo:ogimage"     => SeoOgImage     ?? string.Empty,
                _                 => null
            };

            if (value is not null)
                result[token] = value;
        }

        return Task.FromResult<IReadOnlyDictionary<string, string>>(result);
    }
}
