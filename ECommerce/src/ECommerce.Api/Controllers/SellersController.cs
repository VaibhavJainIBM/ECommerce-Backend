using ECommerce.Application.Common;
using ECommerce.Application.Sellers;
using ECommerce.Application.Sellers.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/sellers")]
public sealed class SellersController(
    ISellerOnboardingService onboardingService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(
        typeof(SellerOnboardingResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SellerOnboardingResponseDto>>
        CreateAsync(
            [FromBody] CreateSellerRequestDto? request,
            CancellationToken cancellationToken)
    {
        var result = await onboardingService.CreateAsync(
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            result.Value!);
    }

    private ActionResult ToProblem(
        IReadOnlyCollection<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new InvalidOperationException(
                "A failed seller result contained no errors.");
        }

        if (Contains(
                errors,
                SellerErrors.CurrentUserUnavailable))
        {
            return ApiProblem(
                StatusCodes.Status401Unauthorized,
                "Authentication failed.",
                SellerErrors.CurrentUserUnavailable.Description,
                SellerErrors.CurrentUserUnavailable.Code);
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
                ["code"] = code,
                ["traceId"] =
                    HttpContext.TraceIdentifier
            });
    }

    private static bool Contains(
        IEnumerable<Error> errors,
        Error expectedError)
    {
        return errors.Any(error =>
            string.Equals(
                error.Code,
                expectedError.Code,
                StringComparison.Ordinal));
    }
}