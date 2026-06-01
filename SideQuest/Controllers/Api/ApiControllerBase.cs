using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        protected ActionResult<T> ToActionResult<T>(ServiceResult<T> result)
        {
            if (result.Status == ServiceResultStatus.Validation)
            {
                var validationDetails = new HttpValidationProblemDetails(
                    result.Errors.ToDictionary(error => error.Key, error => error.Value))
                {
                    Title = result.Message ?? "Validation failed.",
                    Status = StatusCodes.Status400BadRequest
                };

                return BadRequest(validationDetails);
            }

            return result.Status switch
            {
                ServiceResultStatus.Success => Ok(result.Value),
                ServiceResultStatus.Created => StatusCode(StatusCodes.Status201Created, result.Value),
                ServiceResultStatus.NotFound => Problem(result.Message, statusCode: StatusCodes.Status404NotFound),
                ServiceResultStatus.Forbidden => Problem(result.Message, statusCode: StatusCodes.Status403Forbidden),
                ServiceResultStatus.Conflict => Problem(result.Message, statusCode: StatusCodes.Status409Conflict),
                ServiceResultStatus.Unauthorized => Problem(result.Message, statusCode: StatusCodes.Status401Unauthorized),
                _ => Problem("Unexpected service result.", statusCode: StatusCodes.Status500InternalServerError)
            };
        }

        protected ActionResult<T> UnauthorizedResult<T>()
        {
            return Problem("Authentication is required.", statusCode: StatusCodes.Status401Unauthorized);
        }
    }
}
