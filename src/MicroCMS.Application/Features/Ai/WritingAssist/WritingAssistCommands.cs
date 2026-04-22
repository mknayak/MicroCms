using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;

namespace MicroCMS.Application.Features.Ai.WritingAssist;

/// <summary>Draft entry content from a plain-language prompt (GAP-25).</summary>
[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record DraftContentCommand(
    Guid EntryId,
    string Prompt,
    string? Locale = null) : ICommand<AiContentResult>;

/// <summary>Rewrite the current entry field content (GAP-25).</summary>
[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record RewriteContentCommand(
    Guid EntryId,
    string FieldHandle,
  string Instructions) : ICommand<AiContentResult>;

/// <summary>Change the tone of entry content (GAP-25).</summary>
[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record ChangeToneCommand(
    Guid EntryId,
    string FieldHandle,
    WritingTone Tone) : ICommand<AiContentResult>;

/// <summary>Summarise the entry's current content (GAP-25).</summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record SummarizeContentCommand(
    Guid EntryId,
  string FieldHandle,
    int MaxSentences = 3) : IQuery<AiContentResult>;

public enum WritingTone
{
    Professional, Casual, Friendly, Formal, Persuasive, Empathetic
}

/// <summary>AI-generated text returned by Writing Assist commands.</summary>
public sealed record AiContentResult(
    string GeneratedText,
    int PromptTokens,
    int CompletionTokens,
    string ProviderName);
