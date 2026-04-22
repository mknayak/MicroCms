namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Application-layer facade for all LLM operations (GAP-24–27).
/// Routes requests to the configured provider via Ai.Core, enforces PII redaction,
/// and records token usage. All application-layer AI commands depend solely on this
/// interface — never on a concrete provider SDK.
/// ADR-011: provider swap requires only a configuration change.
/// </summary>
public interface ILlmService
{
    /// <summary>Sends a single-turn prompt and returns the full completion.</summary>
    Task<LlmResponse> CompleteAsync(
        LlmRequest request,
      CancellationToken cancellationToken = default);

    /// <summary>Streams completion tokens for real-time UI rendering.</summary>
    IAsyncEnumerable<string> StreamAsync(
        LlmRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Application-level LLM request wrapper.</summary>
public sealed record LlmRequest(
 string SystemPrompt,
    string UserMessage,
  string? FeatureHint = null,      // e.g. "writing_assist", "translation", "quality_check"
    float Temperature = 0.7f,
    int? MaxTokens = null,
    string? ResponseFormat = null);  // "json_object" for structured extraction

/// <summary>Application-level LLM response.</summary>
public sealed record LlmResponse(
    string Content,
    int PromptTokens,
    int CompletionTokens,
    string ProviderName,
    string Model);
