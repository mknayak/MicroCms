using Microsoft.Extensions.Logging;

namespace MicroCMS.Application.Features.Delivery.Rendering.Resolvers;

/// <summary>
/// Handles <c>user:*</c> tokens in shell templates.
///
/// <c>user:*</c> tokens are intentionally <b>not resolved</b> at the shell level because
/// shell templates are shared across requests and may be cached — injecting per-user values
/// would cause data leakage between sessions.
///
/// This resolver logs a <see cref="LogLevel.Warning"/> for each user:* token found and
/// returns an empty dictionary so the pipeline emits an HTML comment in its place.
/// Use <c>user:*</c> only within component-scoped templates where per-request isolation
/// is guaranteed (Sprint 19+ component rendering scope).
/// </summary>
public sealed class UserTokenResolver(ILogger<UserTokenResolver> logger) : ITokenResolver
{
    public string Namespace => "user";

    /// <summary>
    /// <c>false</c> — user context is request-scoped and must never be cached
    /// across different requests or users.
    /// </summary>
    public bool Cacheable => false;

    public Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        IEnumerable<string> tokens,
        RenderContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var token in tokens)
        {
            logger.LogWarning(
                "Token '{Token}' belongs to the user:* namespace. " +
                "user:* tokens are not resolved in shell templates to prevent cross-request data leakage. " +
                "An HTML comment will be emitted in place of this token.",
                token);
        }

        // Return empty — pipeline will emit HTML comment fallbacks.
        return Task.FromResult<IReadOnlyDictionary<string, string>>(
            new Dictionary<string, string>());
    }
}
