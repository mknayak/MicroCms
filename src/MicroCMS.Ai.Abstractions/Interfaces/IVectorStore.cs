using MicroCMS.Ai.Abstractions.Dtos;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Ai.Abstractions.Interfaces;

/// <summary>
/// Provider-agnostic interface for vector store operations (upsert, query, delete).
/// Concrete adapters: PgVector, OpenSearch k-NN, Qdrant.
/// Embeddings are always scoped to a tenant — never mixed.
/// </summary>
public interface IVectorStore
{
    string StoreName { get; }

    Task UpsertAsync(
        TenantId tenantId,
        IReadOnlyList<VectorDocument> documents,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        TenantId tenantId,
        VectorSearchQuery query,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        TenantId tenantId,
        IReadOnlyList<string> documentIds,
        CancellationToken cancellationToken = default);
}
