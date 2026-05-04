using MicroCMS.Ai.Abstractions.Dtos;
using MicroCMS.Ai.Core.Services;
using MicroCMS.Application.Common.Interfaces;
using System.Runtime.CompilerServices;

namespace MicroCMS.Infrastructure.Ai;

/// <summary>
/// Bridges the application-layer <see cref="ILlmService"/> contract to the
/// infrastructure <see cref="AiOrchestrator"/>, which routes requests to the
/// tenant-configured provider (Azure OpenAI, Ollama, etc.).
///
/// ADR-011: Application-layer handlers depend only on <see cref="ILlmService"/>;
/// this adapter is the single point where Ai.Core is wired into the DI graph.
/// </summary>
internal sealed class LlmServiceAdapter(AiOrchestrator orchestrator) : ILlmService
{
    public async Task<LlmResponse> CompleteAsync(
        LlmRequest request,
        MicroCMS.Shared.Ids.SiteId? siteId = null,
        CancellationToken cancellationToken = default)
    {
        var completionRequest = ToCompletionRequest(request);
        var response = await orchestrator.CompleteAsync(completionRequest, siteId, cancellationToken);

        return new LlmResponse(
            Content: response.Content,
            PromptTokens: response.PromptTokens,
            CompletionTokens: response.CompletionTokens,
            ProviderName: response.Model,
            Model: response.Model);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        LlmRequest request,
        MicroCMS.Shared.Ids.SiteId? siteId = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var completionRequest = ToCompletionRequest(request);

        await foreach (var chunk in orchestrator.StreamAsync(completionRequest, siteId, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Delta))
                yield return chunk.Delta;
        }
    }

    // ── Mapping ───────────────────────────────────────────────────────────

    private static CompletionRequest ToCompletionRequest(LlmRequest request)
    {
        var messages = new List<ChatMessage>
        {
            new("system", request.SystemPrompt),
            new("user",   request.UserMessage),
        };

        return new CompletionRequest(
            Messages: messages,
            Model: string.Empty,          // resolved by AiOrchestrator from tenant settings
            Temperature: request.Temperature,
            MaxTokens: request.MaxTokens,
            ResponseFormat: request.ResponseFormat);
    }
}
