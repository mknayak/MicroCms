using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions.Dtos;
using MicroCMS.Ai.Abstractions.Interfaces;

namespace MicroCMS.Ai.Providers.Ollama;

/// <summary>
/// Ollama implementation of <see cref="IAiEmbeddingProvider"/>.
/// Generates text embeddings using local or remote Ollama instances.
/// </summary>
public sealed class OllamaEmbeddingProvider : IAiEmbeddingProvider
{
    private readonly string _endpoint;
    private readonly string _model;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;

    public string ProviderName => "ollama";

    public OllamaEmbeddingProvider(
        string endpoint,
        string model,
        ILogger<OllamaEmbeddingProvider> logger)
    {
        _endpoint = endpoint?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(endpoint));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_endpoint),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<EmbeddingResponse> EmbedAsync(
        EmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Input text cannot be empty.", nameof(request));
        }

        _logger.LogDebug(
            "Sending embedding request to Ollama: model={Model}, text_length={Length}",
            _model,
            request.Text.Length);

        var ollamaRequest = new OllamaEmbeddingRequest
        {
            Model = _model,
            Prompt = request.Text
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/embeddings",
            ollamaRequest,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(
            cancellationToken: cancellationToken);

        if (ollamaResponse?.Embedding == null || ollamaResponse.Embedding.Length == 0)
        {
            throw new InvalidOperationException("Ollama returned empty embedding.");
        }

        _logger.LogDebug(
            "Ollama embedding completed: dimension={Dimension}",
            ollamaResponse.Embedding.Length);

        return new EmbeddingResponse(
            Vector: ollamaResponse.Embedding,
            Model: _model,
            TokenCount: 0); // Ollama doesn't return token counts
    }

    public async Task<IReadOnlyList<EmbeddingResponse>> EmbedBatchAsync(
        IReadOnlyList<EmbeddingRequest> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests, nameof(requests));

        if (requests.Count == 0)
        {
            return Array.Empty<EmbeddingResponse>();
        }

        _logger.LogDebug(
            "Processing batch embedding request: {Count} texts",
            requests.Count);

        // Ollama processes one at a time, so we'll call EmbedAsync for each
        var tasks = requests.Select(r => EmbedAsync(r, cancellationToken));
        var results = await Task.WhenAll(tasks);

        return results;
    }

    #region Internal DTOs

    private sealed class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    #endregion
}
