using MicroCMS.Ai.Abstractions.Dtos;

namespace MicroCMS.Ai.Abstractions.Interfaces;

/// <summary>
/// Provider-agnostic interface for generating vector embeddings.
/// </summary>
public interface IAiEmbeddingProvider
{
    string ProviderName { get; }

    Task<EmbeddingResponse> EmbedAsync(
        EmbeddingRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmbeddingResponse>> EmbedBatchAsync(
        IReadOnlyList<EmbeddingRequest> requests,
        CancellationToken cancellationToken = default);
}
