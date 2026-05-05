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
        var config = await _settingsReader.GetAiProviderConfigAsync(_currentUser.TenantId, siteId, cancellationToken);

        config.ValidateForCompletion();

        var provider = _providerRegistry.GetCompletionProvider(
            config.ProviderName, config.Endpoint, config.ApiKey, config.Model, config.DataResidencyRegion);

        _logger.LogInformation(
            "Routing completion request to provider '{Provider}' for tenant {TenantId}",
            config.ProviderName, _currentUser.TenantId);

        try
        {
            var response = await provider.CompleteAsync(request, cancellationToken);

            _logger.LogInformation(
                "Completion succeeded: {PromptTokens} prompt tokens, {CompletionTokens} completion tokens, provider '{Provider}'",
                response.PromptTokens, response.CompletionTokens, config.ProviderName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Completion failed for provider '{Provider}', tenant {TenantId}",
                config.ProviderName, _currentUser.TenantId);
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
        var config = await _settingsReader.GetAiProviderConfigAsync(
            _currentUser.TenantId, siteId, cancellationToken);

        config.ValidateForCompletion();

        var provider = _providerRegistry.GetCompletionProvider(
            config.ProviderName, config.Endpoint, config.ApiKey, config.Model, config.DataResidencyRegion);

        _logger.LogInformation("Streaming completion from provider '{Provider}' for tenant {TenantId}", config.ProviderName, _currentUser.TenantId);

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
        var config = await _settingsReader.GetAiProviderConfigAsync(
            _currentUser.TenantId, siteId, cancellationToken);

        config.ValidateForEmbedding();

        var provider = _providerRegistry.GetEmbeddingProvider(
            config.ProviderName, config.Endpoint, config.ApiKey, config.Model);

        _logger.LogInformation("Generating embedding with provider '{Provider}' for tenant {TenantId}", config.ProviderName, _currentUser.TenantId);

        try
        {
            return await provider.EmbedAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Embedding generation failed for provider '{Provider}', tenant {TenantId}",
                config.ProviderName, _currentUser.TenantId);
            throw;
        }
    }
}
