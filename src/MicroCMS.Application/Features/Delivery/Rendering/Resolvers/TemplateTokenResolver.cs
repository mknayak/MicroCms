namespace MicroCMS.Application.Features.Delivery.Rendering.Resolvers;

/// <summary>
/// Resolves <c>template:*</c> tokens from the current <see cref="RenderContext"/>.
///
/// Supported tokens:
/// <list type="bullet">
///   <item><c>template:key</c> — machine key of the page template</item>
///   <item><c>template:name</c> — human-readable name of the page template</item>
/// </list>
/// </summary>
public sealed class TemplateTokenResolver : ITokenResolver
{
    public string Namespace => "template";
    public bool Cacheable => true;

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
                "template:key"  => context.TemplateKey  ?? string.Empty,
                "template:name" => context.TemplateName ?? string.Empty,
                _               => null
            };

            if (value is not null)
                result[token] = value;
        }

        return Task.FromResult<IReadOnlyDictionary<string, string>>(result);
    }
}
