using ECommerce.Application.Common;
using ECommerce.Application.Storefront;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/storefront/listings")]
public sealed class StorefrontController(
    IStorefrontService storefrontService)
    : ControllerBase
{
    [HttpGet]
    public async Task<
        ActionResult<PagedStorefrontListingsResponseDto>>
        SearchAsync(
            [FromQuery] StorefrontQueryDto? query,
            CancellationToken cancellationToken)
    {
        var result = await storefrontService.SearchAsync(
            query,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return Ok(result.Value!);
    }

    [HttpGet("{listingId:guid}")]
    public async Task<ActionResult<StorefrontListingResponseDto>>
        GetByIdAsync(
            Guid listingId,
            CancellationToken cancellationToken)
    {
        var result = await storefrontService.GetByIdAsync(
            listingId,
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
        var error = errors.First();

        if (error.Code ==
            StorefrontErrors.ListingNotFoundCode)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Storefront listing not found.",
                detail: error.Description,
                instance: HttpContext.Request.Path,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error.Code
                });
        }

        var groupedErrors = errors
            .GroupBy(item => item.Code)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => item.Description)
                    .Distinct()
                    .ToArray());

        var problemDetails =
            new ValidationProblemDetails(groupedErrors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title =
                    "Invalid storefront query.",
                Detail =
                    "Correct the supplied values and try again.",
                Instance = HttpContext.Request.Path
            };

        return ValidationProblem(problemDetails);
    }
}