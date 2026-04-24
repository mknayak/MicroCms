using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Ai;

/// <summary>Strongly-typed ID for <c>CopilotConversation</c>.</summary>
public readonly record struct CopilotConversationId(Guid Value)
{
    public static CopilotConversationId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Stores the history of an AI Copilot chat session (GAP-24).
/// Messages are append-only. Session statistics (tokens, cost) are tracked here.
/// </summary>
public sealed class CopilotConversation : AggregateRoot<CopilotConversationId>
{
    public const int MaxMessages = 200;

    private readonly List<CopilotMessage> _messages = [];

    private CopilotConversation() : base() { } // EF Core

    private CopilotConversation(CopilotConversationId id, TenantId tenantId, Guid userId)
       : base(id)
    {
    TenantId = tenantId;
    UserId = userId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public bool GroundedOnlyMode { get; private set; }
    public int TotalPromptTokens { get; private set; }
    public int TotalCompletionTokens { get; private set; }
    public decimal TotalCostUsd { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastMessageAt { get; private set; }
  public IReadOnlyList<CopilotMessage> Messages => _messages.AsReadOnly();

    public static CopilotConversation Start(TenantId tenantId, Guid userId, bool groundedOnly = false)
    {
   var conv = new CopilotConversation(CopilotConversationId.New(), tenantId, userId)
   {
    GroundedOnlyMode = groundedOnly
        };
 return conv;
    }

    public CopilotMessage AddUserMessage(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
        return AppendMessage(CopilotMessageRole.User, content);
    }

    public CopilotMessage AddAssistantMessage(
      string content,
 int promptTokens,
    int completionTokens,
        decimal costUsd,
   IEnumerable<CopilotCitation>? citations = null)
    {
     ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
 var msg = AppendMessage(CopilotMessageRole.Assistant, content, citations);
     TotalPromptTokens += promptTokens;
  TotalCompletionTokens += completionTokens;
        TotalCostUsd += costUsd;
 return msg;
    }

    private CopilotMessage AppendMessage(
  CopilotMessageRole role,
  string content,
      IEnumerable<CopilotCitation>? citations = null)
    {
      if (_messages.Count >= MaxMessages)
            throw new DomainException("Conversation has reached the maximum message limit. Start a new session.");

      var citationList = citations?.ToList().AsReadOnly()
 ?? new List<CopilotCitation>().AsReadOnly();

    var msg = new CopilotMessage(Guid.NewGuid(), role, content, citationList, DateTimeOffset.UtcNow);
        _messages.Add(msg);
        LastMessageAt = msg.CreatedAt;
        return msg;
    }
}

public enum CopilotMessageRole { User, Assistant, Tool }

/// <summary>A single message in a copilot conversation.</summary>
public sealed class CopilotMessage
{
    private readonly List<CopilotCitation> _citations = [];

    private CopilotMessage() { } // EF Core

    internal CopilotMessage(Guid id, CopilotMessageRole role, string content, IEnumerable<CopilotCitation> citations, DateTimeOffset createdAt)
    {
    Id = id;
        Role = role;
        Content = content;
    _citations.AddRange(citations);
      CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
public CopilotMessageRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public IReadOnlyList<CopilotCitation> Citations => _citations.AsReadOnly();
    public DateTimeOffset CreatedAt { get; private set; }
}

/// <summary>A source citation produced by RAG retrieval (GAP-24).</summary>
public sealed class CopilotCitation
{
    private CopilotCitation() { } // EF Core

    public CopilotCitation(Guid entryId, string slug, string title, double similarityScore)
    {
        EntryId = entryId;
        Slug = slug;
        Title = title;
SimilarityScore = similarityScore;
    }

    public Guid EntryId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
public double SimilarityScore { get; private set; }
}
