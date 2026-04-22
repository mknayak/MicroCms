using MicroCMS.Application.Features.Ai.Translation;
using MicroCMS.Application.Features.Ai.WritingAssist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// AI-powered writing assistance: draft, rewrite, tone change, summarise, translate (GAP-25/26).
/// </summary>
[Authorize]
public sealed class AiWritingController : ApiControllerBase
{
[HttpPost("{entryId:guid}/draft")]
    [ProducesResponseType(typeof(AiContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Draft(
    Guid entryId, [FromBody] DraftRequest r, CancellationToken ct = default) =>
  OkOrProblem(await Sender.Send(new DraftContentCommand(entryId, r.Prompt, r.Locale), ct));

    [HttpPost("{entryId:guid}/rewrite")]
    [ProducesResponseType(typeof(AiContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Rewrite(
 Guid entryId, [FromBody] RewriteRequest r, CancellationToken ct = default) =>
  OkOrProblem(await Sender.Send(new RewriteContentCommand(entryId, r.FieldHandle, r.Instructions), ct));

    [HttpPost("{entryId:guid}/tone")]
    [ProducesResponseType(typeof(AiContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeTone(
       Guid entryId, [FromBody] ToneRequest r, CancellationToken ct = default) =>
      OkOrProblem(await Sender.Send(new ChangeToneCommand(entryId, r.FieldHandle, r.Tone), ct));

 [HttpGet("{entryId:guid}/summarize")]
    [ProducesResponseType(typeof(AiContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Summarize(
   Guid entryId, [FromQuery] string fieldHandle,
   [FromQuery] int maxSentences = 3, CancellationToken ct = default) =>
  OkOrProblem(await Sender.Send(new SummarizeContentCommand(entryId, fieldHandle, maxSentences), ct));

    [HttpPost("{entryId:guid}/translate")]
    [ProducesResponseType(typeof(MicroCMS.Application.Features.Entries.Dtos.EntryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Translate(
     Guid entryId, [FromBody] TranslateRequest r, CancellationToken ct = default) =>
   OkOrProblem(await Sender.Send(new TranslateEntryLocaleCommand(entryId, r.SourceLocale, r.TargetLocale), ct));
}

public sealed record DraftRequest(string Prompt, string? Locale = null);
public sealed record RewriteRequest(string FieldHandle, string Instructions);
public sealed record ToneRequest(string FieldHandle, WritingTone Tone);
public sealed record TranslateRequest(string SourceLocale, string TargetLocale);
