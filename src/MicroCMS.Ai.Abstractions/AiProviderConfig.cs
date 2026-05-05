using MicroCMS.Ai.Abstractions.Interfaces;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Ai.Abstractions;

/// <summary>
/// Resolved AI provider configuration for a given tenant/site scope.
/// Populated by <see cref="SettingsReaderAiExtensions.GetAiProviderConfigAsync"/>.
/// </summary>
public sealed record AiProviderConfig(
    string ProviderName,
    string Endpoint,
    string ApiKey,
    string? Model,
    string? DataResidencyRegion)
{
    /// <summary>
    /// Validates that required fields are present for non-local providers.
    /// Throws <see cref="InvalidOperationException"/> with a descriptive message on failure.
    /// </summary>
    public void ValidateForCompletion()
    {
        if (string.IsNullOrWhiteSpace(ProviderName))
            throw new InvalidOperationException(
                "AI provider not configured. Set 'ai:provider' in tenant/site settings.");

        if (string.IsNullOrWhiteSpace(Endpoint) && ProviderName != "ollama")
            throw new InvalidOperationException(
                $"AI endpoint not configured for provider '{ProviderName}'. Set 'ai:endpoint' in settings.");

        if (string.IsNullOrWhiteSpace(ApiKey) && ProviderName != "ollama")
            throw new InvalidOperationException(
                $"AI API key not configured for provider '{ProviderName}'. Set 'ai:api_key' in settings.");
    }

    /// <summary>
    /// Validates that required fields are present for embedding providers.
    /// </summary>
    public void ValidateForEmbedding()
    {
        if (string.IsNullOrWhiteSpace(ProviderName) || string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException(
                "AI provider not fully configured for embeddings. Set 'ai:provider' and 'ai:endpoint' in settings.");
    }
}

/// <summary>
/// Extension methods on <see cref="ISettingsReader"/> for reading AI-specific configuration.
/// </summary>
public static class SettingsReaderAiExtensions
{
    /// <summary>
    /// Reads all AI provider settings for the given tenant/site scope in parallel
    /// and returns a single <see cref="AiProviderConfig"/>.
    /// </summary>
    public static async Task<AiProviderConfig> GetAiProviderConfigAsync(
        this ISettingsReader reader,
        TenantId tenantId,
        SiteId? siteId,
        CancellationToken cancellationToken = default)
    {
        var providerTask = reader.GetAsync<string>(
            tenantId, siteId, AiSettingKeys.Provider, defaultValue: "azure_openai", cancellationToken);

        var endpointTask = reader.GetAsync<string>(
            tenantId, siteId, AiSettingKeys.Endpoint, defaultValue: string.Empty, cancellationToken);

        var apiKeyTask = reader.GetAsync<string>(
            tenantId, siteId, AiSettingKeys.ApiKey, defaultValue: string.Empty, cancellationToken);

        var modelTask = reader.GetAsync<string>(
            tenantId, siteId, AiSettingKeys.Model, defaultValue: null, cancellationToken);

        var regionTask = reader.GetAsync<string>(
            tenantId, siteId, AiSettingKeys.DataResidencyRegion, defaultValue: null, cancellationToken);

        await Task.WhenAll(providerTask, endpointTask, apiKeyTask, modelTask, regionTask);

        return new AiProviderConfig(
            ProviderName:        providerTask.Result,
            Endpoint:            endpointTask.Result,
            ApiKey:              apiKeyTask.Result,
            Model:               modelTask.Result,
            DataResidencyRegion: regionTask.Result);
    }
}
