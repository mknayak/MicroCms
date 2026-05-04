using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions;
using MicroCMS.Ai.Abstractions.Dtos;
using MicroCMS.Ai.Abstractions.Interfaces;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Ai.Core.Services;

/// <summary>
/// Routes AI completion and embedding requests to the tenant's configured provider.
/// Reads provider configuration from <see cref="ISettingsReader"/> using <see cref="AiSettingKeys"/>.
/// ADR-011: provider swap requires only a configuration change.
/// </summary>
public sealed class AiOrchestrator
{
    private readonly ISettingsReader _settingsReader;
    private readonly ProviderRegistry _providerRegistry;
    private readonly IAiCurrentUser _currentUser;
    private readonly ILogger<AiOrchestrator> _logger;

    public AiOrchestrator(
        ISettingsReader settingsReader,
        ProviderRegistry providerRegistry,
        IAiCurrentUser currentUser,
        ILogger<AiOrchestrator> logger)
    {
        _settingsReader = settingsReader;
        _providerRegistry = providerRegistry;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Routes a completion request to the configured provider for the current tenant.
    /// </summary>
    public async Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        SiteId? siteId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUser.TenantId;

        // Read provider configuration from settings
        var providerName = await _settingsReader.GetAsync<string>(
            tenantId,
            siteId,
            AiSettingKeys.Provider,
            defaultValue: "azure_openai",
            cancellationToken);

        var endpoint = await _settingsReader.GetAsync<string>(
            tenantId,
            siteId,
            AiSettingKeys.Endpoint,
            defaultValue: string.Empty,
            cancellationToken);

        var apiKey = await _settingsReader.GetAsync<string>(
            tenantId,
            siteId,
            AiSettingKeys.ApiKey,
            defaultValue: string.Empty,
            cancellationToken);

        var dataResidencyRegion = await _settingsReader.GetAsync<string>(
            tenantId,
            siteId,
            AiSettingKeys.DataResidencyRegion,
            defaultValue: null,
            cancellationToken);

        // Validate configuration
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException("AI provider not configured. Set 'ai:provider' in tenant/site settings.");
        }

        if (string.IsNullOrWhiteSpace(endpoint) && providerName != "ollama")
        {
            throw new InvalidOperationException($"AI endpoint not configured for provider '{providerName}'. Set 'ai:endpoint' in settings.");
        }

        if (string.IsNullOrWhiteSpace(apiKey) && providerName != "ollama")
        {
            throw new InvalidOperationException($"AI API key not configured for provider '{providerName}'. Set 'ai:api_key' in settings.");
        }

        // Get or create provider instance
        var provider = _providerRegistry.GetCompletionProvider(
            providerName,
            endpoint,
            apiKey,
            dataResidencyRegion);

        _logger.LogInformation(
            "Routing completion request to provider '{Provider}' for tenant {TenantId}",
            providerName,
            tenantId);

        try
        {
            var response = await provider.CompleteAsync(request, cancellationToken);

            _logger.LogInformation(
                "Completion succeeded: {PromptTokens} prompt tokens, {CompletionTokens} completion tokens, provider '{Provider}'",
                response.PromptTokens,
                response.CompletionTokens,
                providerName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Completion failed for provider '{Provider}', tenant {TenantId}",
                providerName,
                tenantId);
            throw;
        }
    }

    /// <summary>
    /// Streams completion chunks to the configured provider for the current tenant.
    /// </summary>
    public async IAsyncEnumerable<CompletionChunk> StreamAsync(
        CompletionRequest request,
        SiteId? siteId = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUser.TenantId;

        var providerName = await _settingsReader.GetAsync<string>(
            tenantId,
            siteId,
            AiSettingKeys.Provider,
            defaultValue: "azure_openai",
            cancellationToken);

        var endpoint = await _settingsReader.GetAsync<string>(
            tenantId,
            siteId,
            AiSettingKeys.Endpoint,
            defaultValue: string.Empty,
            cancellationToken);

        var apiKey = await _settingsReader.GetAsync<string>(
            tenantId,
            siteId,
            AiSettingKeys.ApiKey,
            defaultValue: string.Empty,
            cancellationToken);

        var dataResidencyRegion = await _settingsReader.GetAsync<string>(
            tenantId,
            siteId,
            AiSettingKeys.DataResidencyRegion,
            defaultValue: null,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException("AI provider not configured.");
        }

        var provider = _providerRegistry.GetCompletionProvider(
            providerName,
            endpoint,
            apiKey,
            dataResidencyRegion);

        _logger.LogInformation(
            "Streaming completion from provider '{Provider}' for tenant {TenantId}",
            providerName,
            tenantId);

        await foreach (var chunk in provider.StreamAsync(request, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Routes an embedding request to the configured provider for the current tenant.
    /// </summary>
    public async Task<EmbeddingResponse> EmbedAsync(
        EmbeddingRequest request,
        SiteId? siteId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUser.TenantId;

        var providerName = await _settingsReader.GetAsync<string>(
            tenantId,
            siteId,
            AiSettingKeys.Provider,
            defaultValue: "azure_openai",
            cancellationToken);

        var endpoint = await _settingsReader.GetAsync<string>(
            tenantId,
            siteId,
            AiSettingKeys.Endpoint,
            defaultValue: string.Empty,
            cancellationToken);

        var apiKey = await _settingsReader.GetAsync<string>(
            tenantId,
            siteId,
            AiSettingKeys.ApiKey,
            defaultValue: string.Empty,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("AI provider not fully configured for embeddings.");
        }

        var provider = _providerRegistry.GetEmbeddingProvider(providerName, endpoint, apiKey);

        _logger.LogInformation(
            "Generating embedding with provider '{Provider}' for tenant {TenantId}",
            providerName,
            tenantId);

        try
        {
            return await provider.EmbedAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Embedding generation failed for provider '{Provider}', tenant {TenantId}",
                providerName,
                tenantId);
            throw;
        }
    }
}
