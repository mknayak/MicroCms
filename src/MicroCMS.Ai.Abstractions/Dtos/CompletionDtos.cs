namespace MicroCMS.Ai.Abstractions.Dtos;

/// <summary>A single message in a chat-style completion request.</summary>
public sealed record ChatMessage(string Role, string Content);

/// <summary>Request sent to <see cref="Interfaces.IAiCompletionProvider"/>.</summary>
public sealed record CompletionRequest(
    IReadOnlyList<ChatMessage> Messages,
    string Model,
    float Temperature = 0.7f,
    int? MaxTokens = null,
    string? ResponseFormat = null,   // "json_object" for structured output
    string? JsonSchema = null);       // ADR-014: caller supplies schema for structured extraction

/// <summary>Non-streaming completion response.</summary>
public sealed record CompletionResponse(
    string Content,
    string Model,
    int PromptTokens,
    int CompletionTokens,
    string FinishReason);

/// <summary>Single chunk in a streaming completion response.</summary>
public sealed record CompletionChunk(
    string Delta,
    bool IsFinal,
    string? FinishReason = null);
