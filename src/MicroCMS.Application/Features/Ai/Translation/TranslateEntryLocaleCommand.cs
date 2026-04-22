using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Mappers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Ai.Translation;

/// <summary>
/// Translates an entry's content to a target locale and saves the result
/// as a new locale variant of the entry (GAP-26).
/// </summary>
[HasPolicy(ContentPolicies.EntryCreate)]
public sealed record TranslateEntryLocaleCommand(
    Guid EntryId,
    string SourceLocale,
    string TargetLocale) : ICommand<EntryDto>;

/// <summary>Handles <see cref="TranslateEntryLocaleCommand"/>.</summary>
public sealed class TranslateEntryLocaleCommandHandler(
    IRepository<Entry, EntryId> entryRepository,
    ILlmService llm,
    ICurrentUser currentUser)
    : IRequestHandler<TranslateEntryLocaleCommand, Result<EntryDto>>
{
    public async Task<Result<EntryDto>> Handle(
  TranslateEntryLocaleCommand request,
    CancellationToken cancellationToken)
    {
     var entry = await entryRepository.GetByIdAsync(new EntryId(request.EntryId), cancellationToken)
  ?? throw new NotFoundException(nameof(Entry), request.EntryId);

  // Translate the FieldsJson content
        var llmRequest = new LlmRequest(
     SystemPrompt: $"Translate the following JSON content values from {request.SourceLocale} to {request.TargetLocale}. " +
             "Preserve the JSON structure exactly. Return only the translated JSON.",
       UserMessage: entry.FieldsJson,
            FeatureHint: "translation",
  Temperature: 0.3f);

        var response = await llm.CompleteAsync(llmRequest, cancellationToken);

  // Create a new entry as the translated locale variant
var targetLocale = Locale.Create(request.TargetLocale);
     var translated = Entry.Create(
      entry.TenantId, entry.SiteId, entry.ContentTypeId,
    entry.Slug, targetLocale, currentUser.UserId,
          fieldsJson: response.Content);

  await entryRepository.AddAsync(translated, cancellationToken);
        return Result.Success(EntryMapper.ToDto(translated));
    }
}
