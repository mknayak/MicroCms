namespace MicroCMS.Ai.Abstractions.Dtos;

public sealed record EmbeddingRequest(string Text, string Model);

public sealed record EmbeddingResponse(
    float[] Vector,
    string Model,
    int TokenCount);
