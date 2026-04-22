using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Ai.WritingAssist;

/// <summary>Handles <see cref="DraftContentCommand"/>.</summary>
public sealed class DraftContentCommandHandler(
    IRepository<Entry, EntryId> entryRepository,
    ILlmService llm)
    : IRequestHandler<DraftContentCommand, Result<AiContentResult>>
{
    public async Task<Result<AiContentResult>> Handle(DraftContentCommand request, CancellationToken cancellationToken)
    {
        // Verify entry exists
    _ = await entryRepository.GetByIdAsync(new EntryId(request.EntryId), cancellationToken)
     ?? throw new NotFoundException(nameof(Entry), request.EntryId);

 var locale = request.Locale ?? "en";
        var llmRequest = new LlmRequest(
          SystemPrompt: $"You are an expert content writer. Write in locale: {locale}. Return only the content text.",
   UserMessage: $"Draft content for: {request.Prompt}",
     FeatureHint: "writing_assist");

  var response = await llm.CompleteAsync(llmRequest, cancellationToken);
   return Result.Success(new AiContentResult(
      response.Content, response.PromptTokens, response.CompletionTokens, response.ProviderName));
    }
}

/// <summary>Handles <see cref="RewriteContentCommand"/>.</summary>
public sealed class RewriteContentCommandHandler(
 IRepository<Entry, EntryId> entryRepository,
  ILlmService llm)
    : IRequestHandler<RewriteContentCommand, Result<AiContentResult>>
{
  public async Task<Result<AiContentResult>> Handle(RewriteContentCommand request, CancellationToken cancellationToken)
    {
     var entry = await entryRepository.GetByIdAsync(new EntryId(request.EntryId), cancellationToken)
   ?? throw new NotFoundException(nameof(Entry), request.EntryId);

 var llmRequest = new LlmRequest(
    SystemPrompt: "You are an expert editor. Return only the rewritten text.",
     UserMessage: $"Rewrite the following content. Instructions: {request.Instructions}\n\nContent:\n{entry.FieldsJson}",
   FeatureHint: "writing_assist");

        var response = await llm.CompleteAsync(llmRequest, cancellationToken);
  return Result.Success(new AiContentResult(
   response.Content, response.PromptTokens, response.CompletionTokens, response.ProviderName));
    }
}

/// <summary>Handles <see cref="ChangeToneCommand"/>.</summary>
public sealed class ChangeToneCommandHandler(
    IRepository<Entry, EntryId> entryRepository,
  ILlmService llm)
    : IRequestHandler<ChangeToneCommand, Result<AiContentResult>>
{
  public async Task<Result<AiContentResult>> Handle(ChangeToneCommand request, CancellationToken cancellationToken)
{
        var entry = await entryRepository.GetByIdAsync(new EntryId(request.EntryId), cancellationToken)
    ?? throw new NotFoundException(nameof(Entry), request.EntryId);

  var llmRequest = new LlmRequest(
   SystemPrompt: $"Rewrite the content with a {request.Tone} tone. Return only the rewritten text.",
        UserMessage: entry.FieldsJson,
    FeatureHint: "writing_assist");

 var response = await llm.CompleteAsync(llmRequest, cancellationToken);
     return Result.Success(new AiContentResult(
    response.Content, response.PromptTokens, response.CompletionTokens, response.ProviderName));
    }
}

/// <summary>Handles <see cref="SummarizeContentCommand"/>.</summary>
public sealed class SummarizeContentCommandHandler(
    IRepository<Entry, EntryId> entryRepository,
  ILlmService llm)
  : IRequestHandler<SummarizeContentCommand, Result<AiContentResult>>
{
    public async Task<Result<AiContentResult>> Handle(SummarizeContentCommand request, CancellationToken cancellationToken)
    {
  var entry = await entryRepository.GetByIdAsync(new EntryId(request.EntryId), cancellationToken)
     ?? throw new NotFoundException(nameof(Entry), request.EntryId);

 var llmRequest = new LlmRequest(
      SystemPrompt: $"Summarize the following content in at most {request.MaxSentences} sentences. Return only the summary.",
   UserMessage: entry.FieldsJson,
   FeatureHint: "writing_assist");

 var response = await llm.CompleteAsync(llmRequest, cancellationToken);
  return Result.Success(new AiContentResult(
   response.Content, response.PromptTokens, response.CompletionTokens, response.ProviderName));
  }
}
