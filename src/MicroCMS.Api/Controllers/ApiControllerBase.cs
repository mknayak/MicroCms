using Asp.Versioning;
using MediatR;
using MicroCMS.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Base class for all MicroCMS API controllers.
/// Applies shared route prefix, versioning, and Result unwrapping helpers.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    /// <summary>MediatR sender resolved lazily to keep constructor signatures clean.</summary>
    protected ISender Sender =>
        _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>Unwraps a <see cref="Result{T}"/> to 200 OK or problem details.</summary>
    protected IActionResult OkOrProblem<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);

    /// <summary>Unwraps a <see cref="Result{T}"/> to 201 Created or problem details.</summary>
    protected IActionResult CreatedOrProblem<T>(Result<T> result, string actionName, object routeValues) =>
        result.IsSuccess
            ? CreatedAtAction(actionName, routeValues, result.Value)
            : ToProblem(result.Error);

    /// <summary>Unwraps a <see cref="Result"/> to 204 No Content or problem details.</summary>
    protected IActionResult NoContentOrProblem(Result result) =>
        result.IsSuccess ? NoContent() : ToProblem(result.Error);

    /// <summary>Converts a domain error to a problem-details result (for use outside OkOrProblem wrappers).</summary>
  protected IActionResult ToProblemResult(Error error) => ToProblem(error);

    private IActionResult ToProblem(Error error) =>
        Problem(title: error.Code, detail: error.Message, statusCode: ErrorTypeToStatus(error.Type));

    private static int ErrorTypeToStatus(ErrorType type) => type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status422UnprocessableEntity
    };
}
