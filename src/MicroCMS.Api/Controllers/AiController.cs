using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MicroCMS.Api.Controllers;
using MicroCMS.Application.Features.Ai.DraftGeneration;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// AI-powered content assistance endpoints.
/// Provides draft generation, rewriting, summarization, SEO assistance, and translation.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/ai")]
[Authorize]
public sealed class AiController : ApiControllerBase
{

    /// <summary>
    /// Generates a content draft from a natural language prompt.
    /// </summary>
    /// <param name="request">Draft generation request containing content type and prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated draft with fields populated according to the content type schema.</returns>
    /// <response code="200">Draft generated successfully.</response>
    /// <response code="400">Invalid request (e.g., unknown content type, empty prompt).</response>
    /// <response code="401">Unauthorized - missing or invalid JWT.</response>
    /// <response code="403">Forbidden - user lacks EditEntry permission.</response>
    /// <response code="429">Too Many Requests - AI budget limit exceeded.</response>
    /// <response code="500">Internal server error during AI processing.</response>
    [HttpPost("drafts/generate")]
    [ProducesResponseType(typeof(GeneratedDraftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateDraft(
        [FromBody] GenerateDraftRequest request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateDraftCommand
        {
            SiteId = request.SiteId,
            ContentTypeId = request.ContentTypeId,
            Prompt = request.Prompt,
            Context = request.Context
        };

        var result = await Sender.Send(command, cancellationToken);

        return OkOrProblem(result);
    }

    /// <summary>
    /// Request model for draft generation.
    /// </summary>
    public sealed class GenerateDraftRequest
    {
        /// <summary>ID of the site this draft belongs to.</summary>
        public required Guid SiteId { get; init; }

        /// <summary>
        /// ID of the content type to generate content for.
        /// </summary>
        public required Guid ContentTypeId { get; init; }

        /// <summary>
        /// Natural language prompt describing the desired content.
        /// Example: "Write a blog post about clean architecture in .NET"
        /// </summary>
        public required string Prompt { get; init; }

        /// <summary>
        /// Optional context dictionary to guide generation.
        /// Example: { "tone": "professional", "length": "medium", "target_audience": "developers" }
        /// </summary>
        public Dictionary<string, object>? Context { get; init; }
    }
}
