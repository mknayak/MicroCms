using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions.Dtos;
using MicroCMS.Ai.Abstractions.Interfaces;

namespace MicroCMS.Ai.Providers.AzureOpenAI;

/// <summary>
/// Azure OpenAI embedding provider.
/// TODO: Implement against the actual Azure.AI.OpenAI SDK in a follow-up.
/// This is a stub to unblock Sprint 14 core functionality.
/// </summary>
public sealed class AzureOpenAIEmbeddingProvider : IAiEmbeddingProvider
{
    private readonly ILogger<AzureOpenAIEmbeddingProvider> _logger;

    public string ProviderName => "azure_openai";

    public AzureOpenAIEmbeddingProvider(
        string endpoint,
        string apiKey,
        string deploymentName,
        ILogger<AzureOpenAIEmbeddingProvider> logger)
    {
        _logger = logger;
        _logger.LogWarning(
            "Azure OpenAI embedding provider is a stub. Implement against Azure.AI.OpenAI SDK.");
    }

    public Task<EmbeddingResponse> EmbedAsync(
        EmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "Azure OpenAI embedding provider not yet implemented. Use Ollama for Sprint 14.");
    }

    public Task<IReadOnlyList<EmbeddingResponse>> EmbedBatchAsync(
        IReadOnlyList<EmbeddingRequest> requests,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "Azure OpenAI embedding batch not yet implemented. Use Ollama for Sprint 14.");
    }
}
