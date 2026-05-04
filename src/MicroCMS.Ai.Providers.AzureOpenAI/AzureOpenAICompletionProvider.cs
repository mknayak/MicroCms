using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions.Dtos;
using MicroCMS.Ai.Abstractions.Interfaces;

namespace MicroCMS.Ai.Providers.AzureOpenAI;

/// <summary>
/// Azure OpenAI completion provider.
/// TODO: Implement against the actual Azure.AI.OpenAI SDK in a follow-up.
/// This is a stub to unblock Sprint 14 core functionality.
/// </summary>
public sealed class AzureOpenAICompletionProvider : IAiCompletionProvider
{
    private readonly ILogger<AzureOpenAICompletionProvider> _logger;

    public string ProviderName => "azure_openai";

    public AzureOpenAICompletionProvider(
        string endpoint,
        string apiKey,
        string deploymentName,
        ILogger<AzureOpenAICompletionProvider> logger)
    {
        _logger = logger;
        _logger.LogWarning(
            "Azure OpenAI provider is a stub. Implement against Azure.AI.OpenAI SDK.");
    }

    public Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "Azure OpenAI completion provider not yet implemented. Use Ollama for Sprint 14.");
    }

    public IAsyncEnumerable<CompletionChunk> StreamAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "Azure OpenAI streaming not yet implemented. Use Ollama for Sprint 14.");
    }
}
