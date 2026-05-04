using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions.Interfaces;

namespace MicroCMS.Ai.Core.Services;

/// <summary>
/// Registry that manages provider instances and enforces data residency rules.
/// Providers are cached per (providerName, endpoint, apiKey) tuple to avoid recreating instances.
/// Thread-safe for concurrent access.
/// </summary>
public sealed class ProviderRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProviderRegistry> _logger;

    // Cache key: (providerName, endpoint, apiKeyHash)
    private readonly ConcurrentDictionary<string, IAiCompletionProvider> _completionProviders = new();
    private readonly ConcurrentDictionary<string, IAiEmbeddingProvider> _embeddingProviders = new();

    // Maps provider names to factory functions
    private readonly Dictionary<string, Func<string, string, IAiCompletionProvider>> _completionFactories = new();
    private readonly Dictionary<string, Func<string, string, IAiEmbeddingProvider>> _embeddingFactories = new();

    public ProviderRegistry(IServiceProvider serviceProvider, ILogger<ProviderRegistry> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Registers a completion provider factory for a given provider name.
    /// </summary>
    public void RegisterCompletionProvider(
        string providerName,
        Func<string, string, IAiCompletionProvider> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        _completionFactories[providerName.ToLowerInvariant()] = factory;
        _logger.LogInformation("Registered completion provider factory for '{Provider}'", providerName);
    }

    /// <summary>
    /// Registers an embedding provider factory for a given provider name.
    /// </summary>
    public void RegisterEmbeddingProvider(
        string providerName,
        Func<string, string, IAiEmbeddingProvider> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        _embeddingFactories[providerName.ToLowerInvariant()] = factory;
        _logger.LogInformation("Registered embedding provider factory for '{Provider}'", providerName);
    }

    /// <summary>
    /// Gets or creates a completion provider instance for the specified configuration.
    /// Enforces data residency rules if specified.
    /// </summary>
    public IAiCompletionProvider GetCompletionProvider(
        string providerName,
        string endpoint,
        string apiKey,
        string? dataResidencyRegion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));

        var normalizedProviderName = providerName.ToLowerInvariant();
        var cacheKey = BuildCacheKey(normalizedProviderName, endpoint, apiKey);

        return _completionProviders.GetOrAdd(cacheKey, _ =>
        {
            if (!_completionFactories.TryGetValue(normalizedProviderName, out var factory))
            {
                throw new InvalidOperationException(
                    $"No completion provider factory registered for '{providerName}'. " +
                    $"Available providers: {string.Join(", ", _completionFactories.Keys)}");
            }

            _logger.LogInformation(
                "Creating new completion provider instance for '{Provider}' with endpoint '{Endpoint}'",
                providerName,
                MaskEndpoint(endpoint));

            // Validate data residency if specified
            if (!string.IsNullOrWhiteSpace(dataResidencyRegion))
            {
                ValidateDataResidency(endpoint, dataResidencyRegion);
            }

            return factory(endpoint, apiKey);
        });
    }

    /// <summary>
    /// Gets or creates an embedding provider instance for the specified configuration.
    /// </summary>
    public IAiEmbeddingProvider GetEmbeddingProvider(
        string providerName,
        string endpoint,
        string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));

        var normalizedProviderName = providerName.ToLowerInvariant();
        var cacheKey = BuildCacheKey(normalizedProviderName, endpoint, apiKey);

        return _embeddingProviders.GetOrAdd(cacheKey, _ =>
        {
            if (!_embeddingFactories.TryGetValue(normalizedProviderName, out var factory))
            {
                throw new InvalidOperationException(
                    $"No embedding provider factory registered for '{providerName}'. " +
                    $"Available providers: {string.Join(", ", _embeddingFactories.Keys)}");
            }

            _logger.LogInformation(
                "Creating new embedding provider instance for '{Provider}' with endpoint '{Endpoint}'",
                providerName,
                MaskEndpoint(endpoint));

            return factory(endpoint, apiKey);
        });
    }

    private static string BuildCacheKey(string providerName, string endpoint, string apiKey)
    {
        // Hash the API key to avoid storing it in memory as plain text
        var apiKeyHash = string.IsNullOrWhiteSpace(apiKey)
            ? "none"
            : Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(apiKey)))[..16];

        return $"{providerName}::{endpoint}::{apiKeyHash}";
    }

    private static string MaskEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return "[empty]";

        try
        {
            var uri = new Uri(endpoint);
            return $"{uri.Scheme}://{uri.Host}/***";
        }
        catch
        {
            return "[invalid-uri]";
        }
    }

    /// <summary>
    /// Validates that the endpoint complies with the specified data residency region.
    /// Throws if the endpoint is outside the allowed region.
    /// </summary>
    private void ValidateDataResidency(string endpoint, string requiredRegion)
    {
        // Simple heuristic: check if endpoint contains the region string
        // Production implementation should use a more robust region mapping
        if (!endpoint.Contains(requiredRegion, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Data residency violation: endpoint '{Endpoint}' does not match required region '{Region}'",
                MaskEndpoint(endpoint),
                requiredRegion);

            throw new InvalidOperationException(
                $"Data residency constraint violation: endpoint must be in region '{requiredRegion}'");
        }

        _logger.LogInformation(
            "Data residency validation passed for region '{Region}'",
            requiredRegion);
    }

    /// <summary>
    /// Clears all cached provider instances. Useful for testing or configuration changes.
    /// </summary>
    public void ClearCache()
    {
        _completionProviders.Clear();
        _embeddingProviders.Clear();
        _logger.LogInformation("Provider cache cleared");
    }
}
