using ECommerce.Api.Authorization;
using ECommerce.Application.Common;
using ECommerce.Application.Listings;
using ECommerce.Application.Listings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize(Policy = SellerPolicies.Owner)]
[ApiController]
[Route("api/sellers/{sellerId:guid}/listings")]
public sealed class SellerListingsController(
    ISellerListingService listingService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(
        typeof(SellerListingResponseDto),
        StatusCodes.Status201Created)]
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
    public async Task<ActionResult<SellerListingResponseDto>>
        CreateAsync(
            Guid sellerId,
            [FromBody] CreateSellerListingRequestDto? request,
            CancellationToken cancellationToken)
    {
        var result = await listingService.CreateAsync(
            sellerId,
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

    [HttpPatch("{listingId:guid}/price")]
    [ProducesResponseType(
    typeof(SellerListingResponseDto),
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
    public async Task<ActionResult<SellerListingResponseDto>>
    UpdatePriceAsync(
        Guid sellerId,
        Guid listingId,
        [FromBody]
        UpdateSellerListingPriceRequestDto? request,
        CancellationToken cancellationToken)
    {
        var result = await listingService.UpdatePriceAsync(
            sellerId,
            listingId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return Ok(result.Value!);
    }

    [HttpPost("{listingId:guid}/archive")]
    [ProducesResponseType(
    typeof(SellerListingResponseDto),
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
    public async Task<ActionResult<SellerListingResponseDto>>
    ArchiveAsync(
        Guid sellerId,
        Guid listingId,
        [FromBody]
        ArchiveSellerListingRequestDto? request,
        CancellationToken cancellationToken)
    {
        var result = await listingService.ArchiveAsync(
            sellerId,
            listingId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return Ok(result.Value!);
    }

    [HttpGet]
    [ProducesResponseType(
    typeof(PagedSellerListingsResponseDto),
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
    public async Task<
    ActionResult<PagedSellerListingsResponseDto>>
    GetForSellerAsync(
        Guid sellerId,
        [FromQuery] SellerListingQueryDto? query,
        CancellationToken cancellationToken)
    {
        var result = await listingService.GetForSellerAsync(
            sellerId,
            query,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return Ok(result.Value!);
    }

    [HttpGet("{listingId:guid}")]
    [ProducesResponseType(
    typeof(SellerListingResponseDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SellerListingResponseDto>>
    GetByIdAsync(
        Guid sellerId,
        Guid listingId,
        CancellationToken cancellationToken)
    {
        var result = await listingService.GetByIdAsync(
            sellerId,
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
        if (errors.Count == 0)
        {
            throw new InvalidOperationException(
                "A failed listing result contained no errors.");
        }

        var error = errors.First();

        if (error.Code ==
                SellerListingErrors.SellerNotFoundCode ||
            error.Code ==
                SellerListingErrors.VariantNotFoundCode ||
            error.Code ==
                SellerListingErrors.ListingNotFoundCode)
        {
            return ApiProblem(
                StatusCodes.Status404NotFound,
                "Resource not found.",
                error.Description,
                error.Code);
        }

        if (error.Code ==
                SellerListingErrors.SellerUnavailableCode ||
            error.Code ==
                SellerListingErrors.CatalogUnavailableCode ||
            error.Code ==
                SellerListingErrors.DuplicateSellerSkuCode ||
            error.Code ==
                SellerListingErrors.DuplicateSellerVariantCode ||
            error.Code ==
                SellerListingErrors.ListingStateConflictCode ||
            error.Code ==
                SellerListingErrors.ConcurrencyConflictCode)
        {
            return ApiProblem(
                StatusCodes.Status409Conflict,
                "Listing conflict.",
                error.Description,
                error.Code);
        }

        var groupedErrors = errors
            .GroupBy(item => item.Code)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => item.Description)
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