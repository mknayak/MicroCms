using Asp.Versioning;
using MediatR;
using MicroCMS.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Delivery.WebHost.Controllers;

/// <summary>
/// Base for all delivery API controllers.
/// Route: <c>/delivery/v{version}/[controller]</c>
/// Auth: <c>X-Api-Key</c> header (DeliveryApiKey scheme).
/// </summary>
[ApiController]
[Route("delivery/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public abstract class DeliveryControllerBase : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender =>
      _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult OkOrProblem<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);

    private IActionResult ToProblem(Error error) =>
        Problem(
       title: error.Code,
  detail: error.Message,
            statusCode: error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError,
            });
}
