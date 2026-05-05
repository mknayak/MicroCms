using System.ClientModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions.Dtos;
using MicroCMS.Ai.Abstractions.Interfaces;
using OpenAI;
using OpenAI.Chat;

namespace MicroCMS.Ai.Providers.OpenAI;

/// <summary>
/// OpenAI completion provider using the official OpenAI .NET SDK (v2).
/// Supports both non-streaming and streaming chat completions.
/// Set provider name to "openai" in tenant/site settings.
/// </summary>
public sealed class OpenAICompletionProvider : IAiCompletionProvider
{
    private readonly ChatClient _chatClient;
    private readonly string _model;
    private readonly ILogger<OpenAICompletionProvider> _logger;

    public string ProviderName => "openai";

    public OpenAICompletionProvider(
        string model,
        string apiKey,
        ILogger<OpenAICompletionProvider> logger,
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
        _chatClient = openAiClient.GetChatClient(model);
    }

    public async Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messages = BuildMessages(request);
        var options = BuildOptions(request);

        _logger.LogDebug("Sending completion request to OpenAI with model '{Model}'", _model);

        var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

        var completion = response.Value;
        var content = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;

        var result = new CompletionResponse(
            Content: content,
            Model: _model,
            PromptTokens: completion.Usage.InputTokenCount,
            CompletionTokens: completion.Usage.OutputTokenCount,
            FinishReason: completion.FinishReason.ToString() ?? "stop");

        _logger.LogDebug(
            "OpenAI completion succeeded: {PromptTokens} prompt + {CompletionTokens} completion tokens",
            result.PromptTokens, result.CompletionTokens);

        return result;
    }

    public async IAsyncEnumerable<CompletionChunk> StreamAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messages = BuildMessages(request);
        var options = BuildOptions(request);

        _logger.LogDebug("Starting streaming completion from OpenAI with model '{Model}'", _model);

        var stream = _chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken);

        await foreach (var update in stream.WithCancellation(cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return new CompletionChunk(
                        Delta: part.Text,
                        IsFinal: false);
                }
            }

            if (update.FinishReason is not null)
            {
                yield return new CompletionChunk(
                    Delta: string.Empty,
                    IsFinal: true,
                    FinishReason: update.FinishReason.ToString());
            }
        }

        _logger.LogDebug("OpenAI streaming completion finished");
    }

    private static List<global::OpenAI.Chat.ChatMessage> BuildMessages(CompletionRequest request)
    {
        var messages = new List<global::OpenAI.Chat.ChatMessage>(request.Messages.Count);
        foreach (var m in request.Messages)
        {
            messages.Add(m.Role.ToLowerInvariant() switch
            {
                "system"    => new SystemChatMessage(m.Content),
                "assistant" => new AssistantChatMessage(m.Content),
                _           => new UserChatMessage(m.Content),
            });
        }
        return messages;
    }

    private ChatCompletionOptions BuildOptions(CompletionRequest request)
    {
        var options = new ChatCompletionOptions
        {
            Temperature = request.Temperature,
        };

        if (request.MaxTokens.HasValue)
            options.MaxOutputTokenCount = request.MaxTokens.Value;

        if (request.ResponseFormat == "json_object")
            options.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();

        return options;
    }
}
