using MicroCMS.Ai.Abstractions.Dtos;

namespace MicroCMS.Ai.Abstractions.Interfaces;

/// <summary>
/// Provider-agnostic interface for text and chat completion.
/// Concrete adapters (Azure OpenAI, OpenAI, Anthropic, etc.) implement this contract.
/// The core domain and application layers never import a vendor SDK — only this interface.
/// </summary>
public interface IAiCompletionProvider
{
    string ProviderName { get; }

    Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<CompletionChunk> StreamAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default);
}
