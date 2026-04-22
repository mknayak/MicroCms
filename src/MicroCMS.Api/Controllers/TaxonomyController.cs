using MicroCMS.Application.Features.Taxonomy.Commands;
using MicroCMS.Application.Features.Taxonomy.Dtos;
using MicroCMS.Application.Features.Taxonomy.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Categories and tags taxonomy management.</summary>
[Authorize]
public sealed class TaxonomyController : ApiControllerBase
{
    // ── Categories ────────────────────────────────────────────────────────

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCategories(
     [FromQuery] Guid siteId,
        CancellationToken cancellationToken = default)
    {
    var result = await Sender.Send(new ListCategoriesQuery(siteId), cancellationToken);
     return OkOrProblem(result);
    }

    [HttpPost("categories")]
  [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryCommand command,
  CancellationToken cancellationToken = default)
    {
      var result = await Sender.Send(command, cancellationToken);
     return result.IsSuccess
   ? StatusCode(StatusCodes.Status201Created, result.Value)
      : OkOrProblem(result);
    }

    [HttpDelete("categories/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken = default)
    {
     var result = await Sender.Send(new DeleteCategoryCommand(id), cancellationToken);
      return NoContentOrProblem(result);
    }

    // ── Tags ──────────────────────────────────────────────────────────────

    [HttpGet("tags")]
    [ProducesResponseType(typeof(IReadOnlyList<TagDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTags(
     [FromQuery] Guid siteId,
      CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ListTagsQuery(siteId), cancellationToken);
     return OkOrProblem(result);
    }

 [HttpPost("tags")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTag(
  [FromBody] CreateTagCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(command, cancellationToken);
     return result.IsSuccess
           ? StatusCode(StatusCodes.Status201Created, result.Value)
    : OkOrProblem(result);
    }

    [HttpDelete("tags/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken = default)
    {
    var result = await Sender.Send(new DeleteTagCommand(id), cancellationToken);
     return NoContentOrProblem(result);
    }
}
