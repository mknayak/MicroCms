using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions.Dtos;
using MicroCMS.Ai.Abstractions.Interfaces;

namespace MicroCMS.Ai.Providers.Ollama;

/// <summary>
/// Ollama implementation of <see cref="IAiCompletionProvider"/>.
/// Communicates with local or remote Ollama instances via the HTTP API.
/// Default endpoint: http://localhost:11434
/// </summary>
public sealed class OllamaCompletionProvider : IAiCompletionProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaCompletionProvider> _logger;
    private readonly string _model;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ProviderName => "ollama";

    public OllamaCompletionProvider(
        string endpoint,
        string model,
        ILogger<OllamaCompletionProvider> logger,
        string? apiKey = null,
        HttpClient? httpClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));

        _httpClient = httpClient ?? new HttpClient();

        var baseAddress = string.IsNullOrWhiteSpace(endpoint)
            ? "http://localhost:11434/"
            : endpoint.TrimEnd('/') + '/';

        _httpClient.BaseAddress = new Uri(baseAddress);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);

        if (!string.IsNullOrWhiteSpace(apiKey))
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        _model = model;
        _logger = logger;
    }

    public async Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        try
        {
            var ollamaRequest = BuildOllamaRequest(request, stream: false);

            _logger.LogDebug("Sending completion request to Ollama with model '{Model}'", _model);

            var response = await _httpClient.PostAsJsonAsync(
                "api/chat", ollamaRequest, JsonOptions, cancellationToken);

            response.EnsureSuccessStatusCode();

            var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>(
                JsonOptions, cancellationToken);

            if (ollamaResponse is null)
                throw new InvalidOperationException("Ollama returned null response.");

            var result = new CompletionResponse(
                Content: ollamaResponse.Message?.Content ?? string.Empty,
                Model: _model,
                PromptTokens: ollamaResponse.PromptEvalCount ?? 0,
                CompletionTokens: ollamaResponse.EvalCount ?? 0,
                FinishReason: ollamaResponse.Done ? "stop" : "unknown");

            _logger.LogDebug(
                "Ollama completion succeeded: {PromptTokens} prompt + {CompletionTokens} completion tokens",
                result.PromptTokens, result.CompletionTokens);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ollama HTTP request failed: {Message}", ex.Message);
            throw new InvalidOperationException($"Ollama request failed: {ex.Message}", ex);
        }
    }

    public async IAsyncEnumerable<CompletionChunk> StreamAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var ollamaRequest = BuildOllamaRequest(request, stream: true);

        _logger.LogDebug("Starting streaming completion from Ollama with model '{Model}'", _model);

        var response = await _httpClient.PostAsJsonAsync(
            "api/chat", ollamaRequest, JsonOptions, cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var chunk = JsonSerializer.Deserialize<OllamaResponse>(line, JsonOptions);

            if (chunk?.Message?.Content is { Length: > 0 } content)
            {
                yield return new CompletionChunk(
                    Delta: content,
                    IsFinal: chunk.Done,
                    FinishReason: chunk.Done ? "stop" : null);
            }

            if (chunk?.Done == true)
                break;
        }

        _logger.LogDebug("Ollama streaming completion finished");
    }

    private OllamaRequest BuildOllamaRequest(CompletionRequest request, bool stream) =>
        new()
        {
            Model = _model,
            Messages = request.Messages.Select(m => new OllamaMessage
            {
                Role = m.Role.ToLowerInvariant(),
                Content = m.Content
            }).ToList(),
            Stream = stream,
            Format = request.ResponseFormat == "json_object" ? "json" : null,
            Options = new OllamaOptions
            {
                Temperature = request.Temperature,
                NumPredict = request.MaxTokens
            }
        };

    #region Ollama DTOs

    private sealed class OllamaRequest
    {
        public required string Model { get; init; }
        public required List<OllamaMessage> Messages { get; init; }
        public bool Stream { get; init; }
        public OllamaOptions? Options { get; init; }
        public string? Format { get; init; }
    }

    private sealed class OllamaMessage
    {
        public required string Role { get; init; }
        public required string Content { get; init; }
    }

    private sealed class OllamaOptions
    {
        public float? Temperature { get; set; }

        [JsonPropertyName("num_predict")]
        public int? NumPredict { get; set; }
    }

    private sealed class OllamaResponse
    {
        public OllamaMessage? Message { get; init; }
        public bool Done { get; init; }

        [JsonPropertyName("prompt_eval_count")]
        public int? PromptEvalCount { get; init; }

        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; init; }
    }

    #endregion
}
