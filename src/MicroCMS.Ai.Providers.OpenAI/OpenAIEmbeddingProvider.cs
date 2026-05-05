using System.ClientModel;
using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions.Dtos;
using MicroCMS.Ai.Abstractions.Interfaces;
using OpenAI;
using OpenAI.Embeddings;

namespace MicroCMS.Ai.Providers.OpenAI;

/// <summary>
/// OpenAI embedding provider using the official OpenAI .NET SDK (v2).
/// Default embedding model: text-embedding-3-small.
/// Set provider name to "openai" in tenant/site settings.
/// </summary>
public sealed class OpenAIEmbeddingProvider : IAiEmbeddingProvider
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly string _model;
    private readonly ILogger<OpenAIEmbeddingProvider> _logger;

    public string ProviderName => "openai";

    public OpenAIEmbeddingProvider(
        string model,
        string apiKey,
        ILogger<OpenAIEmbeddingProvider> logger,
        string? endpoint = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        _model = model;
        _logger = logger;

        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(endpoint))
            clientOptions.Endpoint = new Uri(endpoint);

        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        _embeddingClient = openAiClient.GetEmbeddingClient(model);
    }

    public async Task<EmbeddingResponse> EmbedAsync(
        EmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Text))
            throw new ArgumentException("Input text cannot be empty.", nameof(request));

        _logger.LogDebug(
            "Sending embedding request to OpenAI: model={Model}, text_length={Length}",
            _model, request.Text.Length);

        var response = await _embeddingClient.GenerateEmbeddingsAsync(
            new[] { request.Text }, cancellationToken: cancellationToken);

        var collection = response.Value;
        var vector = collection[0].ToFloats().ToArray();

        _logger.LogDebug("OpenAI embedding completed: dimension={Dimension}", vector.Length);

        return new EmbeddingResponse(
            Vector: vector,
            Model: _model,
            TokenCount: collection.Usage.InputTokenCount);
    }

    public async Task<IReadOnlyList<EmbeddingResponse>> EmbedBatchAsync(
        IReadOnlyList<EmbeddingRequest> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (requests.Count == 0)
            return Array.Empty<EmbeddingResponse>();

        var inputs = requests.Select(r => r.Text).ToList();

        _logger.LogDebug(
            "Sending batch embedding request to OpenAI: model={Model}, count={Count}",
            _model, inputs.Count);

        var response = await _embeddingClient.GenerateEmbeddingsAsync(inputs, cancellationToken: cancellationToken);
        var collection = response.Value;
        var tokenCount = collection.Usage.InputTokenCount;

        return collection
            .Select(e => new EmbeddingResponse(
                Vector: e.ToFloats().ToArray(),
                Model: _model,
                TokenCount: tokenCount))
            .ToList();
    }
}
