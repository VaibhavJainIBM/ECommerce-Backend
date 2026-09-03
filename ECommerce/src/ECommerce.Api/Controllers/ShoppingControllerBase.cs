using ECommerce.Application.Common;
using ECommerce.Application.Shopping;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

public abstract class ShoppingControllerBase : ControllerBase
{
    protected ActionResult ShoppingProblem(IReadOnlyCollection<Error> errors)
    {
        var error = errors.First();
        var status = error.Code switch
        {
            ShoppingErrors.UnauthenticatedCode => StatusCodes.Status401Unauthorized,
            ShoppingErrors.AccountUnavailableCode => StatusCodes.Status403Forbidden,
            ShoppingErrors.NotFoundCode => StatusCodes.Status404NotFound,
            ShoppingErrors.ConflictCode => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Problem(statusCode: status, title: "Shopping request failed.",
            detail: string.Join(" ", errors.Select(item => item.Description).Distinct()),
            instance: HttpContext.Request.Path,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
