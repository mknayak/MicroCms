using MicroCMS.Ai.Abstractions.Dtos;

namespace MicroCMS.Ai.Abstractions.Interfaces;

/// <summary>
/// Provider-agnostic interface for content safety / moderation.
/// Used for pre-call PII redaction validation and post-call safety classification.
/// </summary>
public interface IAiModerationProvider
{
    string ProviderName { get; }

    Task<ModerationResponse> ModerateAsync(
        ModerationRequest request,
        CancellationToken cancellationToken = default);
}
