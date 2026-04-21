namespace MicroCMS.Ai.Abstractions.Dtos;

public sealed record TranscriptionRequest(
    Stream AudioStream,
    string FileName,
    string Model,
    string? Language = null);

public sealed record TranscriptionResponse(
    string Text,
    string? DetectedLanguage,
    TimeSpan Duration);
