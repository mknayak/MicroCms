using MicroCMS.Ai.Abstractions.Dtos;

namespace MicroCMS.Ai.Abstractions.Interfaces;

/// <summary>
/// Provider-agnostic interface for audio/video transcription.
/// </summary>
public interface IAiTranscriptionProvider
{
    string ProviderName { get; }

    Task<TranscriptionResponse> TranscribeAsync(
        TranscriptionRequest request,
        CancellationToken cancellationToken = default);
}
