using ECommerce.Application.Authorization;
using ECommerce.Application.Common;
using ECommerce.Application.Sellers;
using ECommerce.Application.Sellers.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize(Roles = PlatformRoleNames.PlatformAdmin)]
[ApiController]
[Route("api/admin/sellers")]
public sealed class AdminSellersController(
    ISellerLifecycleService lifecycleService)
    : ControllerBase
{
    [HttpPost("{sellerId:guid}/approve")]
    [ProducesResponseType(
        typeof(SellerLifecycleResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SellerLifecycleResponseDto>>
        ApproveAsync(
            Guid sellerId,
            CancellationToken cancellationToken)
    {
        var result = await lifecycleService.ApproveAsync(
            sellerId,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return Ok(result.Value!);
    }

    private ActionResult ToProblem(
        IReadOnlyCollection<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new InvalidOperationException(
                "A failed seller lifecycle result " +
                "contained no errors.");
        }

        var error = errors.First();

        if (error.Code ==
            SellerLifecycleErrors.SellerNotFoundCode)
        {
            return ApiProblem(
                StatusCodes.Status404NotFound,
                "Seller not found.",
                error.Description,
                error.Code);
        }

        if (error.Code ==
            SellerLifecycleErrors.StateConflictCode)
        {
            return ApiProblem(
                StatusCodes.Status409Conflict,
                "Seller state conflict.",
                error.Description,
                error.Code);
        }

        return ValidationProblemResponse(errors);
    }

    private ActionResult ValidationProblemResponse(
        IReadOnlyCollection<Error> errors)
    {
        var groupedErrors = errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.Description)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        var problemDetails =
            new ValidationProblemDetails(groupedErrors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title =
                    "One or more validation errors occurred.",
                Detail =
                    "Correct the supplied values and try again.",
                Instance = HttpContext.Request.Path
            };

        problemDetails.Extensions["traceId"] =
            HttpContext.TraceIdentifier;

        return ValidationProblem(problemDetails);
    }

    private ObjectResult ApiProblem(
        int statusCode,
        string title,
        string detail,
        string code)
    {
        return Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            instance: HttpContext.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code
            });
    }
}