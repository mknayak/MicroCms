namespace MicroCMS.Ai.Abstractions.Dtos;

public sealed record VectorDocument(
    string Id,
    float[] Vector,
    string Content,
    IReadOnlyDictionary<string, object> Metadata);

public sealed record VectorSearchQuery(
    float[] QueryVector,
    int TopK = 10,
    float MinScore = 0.7f,
    IReadOnlyDictionary<string, object>? Filter = null);

public sealed record VectorSearchResult(
    string Id,
    float Score,
    string Content,
    IReadOnlyDictionary<string, object> Metadata);
