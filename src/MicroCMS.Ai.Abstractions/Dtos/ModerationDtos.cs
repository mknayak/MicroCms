namespace MicroCMS.Ai.Abstractions.Dtos;

public sealed record ModerationRequest(string Text, string? Context = null);

public sealed record ModerationResponse(
    bool IsFlagged,
    IReadOnlyDictionary<string, float> CategoryScores,
    string? FlaggedCategory = null);
