using ECommerce.Api.Authorization;
using ECommerce.Application.Common;
using ECommerce.Application.Warehouses;
using ECommerce.Application.Warehouses.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize(Policy = SellerPolicies.Owner)]
[ApiController]
[Route("api/sellers/{sellerId:guid}/warehouses")]
public sealed class WarehousesController(
    IWarehouseService warehouseService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(
        typeof(WarehouseResponseDto),
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
    public async Task<ActionResult<WarehouseResponseDto>>
        CreateAsync(
            Guid sellerId,
            [FromBody] CreateWarehouseRequestDto? request,
            CancellationToken cancellationToken)
    {
        var result = await warehouseService.CreateAsync(
            sellerId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        var response = result.Value!;

        return CreatedAtRoute(
            GetWarehouseByIdRouteName,
            new
            {
                sellerId,
                warehouseId = response.WarehouseId
            },
            response);
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(WarehouseResponseDto[]),
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
    public async Task<
        ActionResult<IReadOnlyCollection<WarehouseResponseDto>>>
        GetForSellerAsync(
            Guid sellerId,
            CancellationToken cancellationToken)
    {
        var result =
            await warehouseService.GetForSellerAsync(
                sellerId,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return Ok(result.Value!);
    }

    [HttpGet(
    "{warehouseId:guid}",
    Name = GetWarehouseByIdRouteName)]
    [ProducesResponseType(
        typeof(WarehouseResponseDto),
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
    public async Task<ActionResult<WarehouseResponseDto>>
        GetByIdAsync(
            Guid sellerId,
            Guid warehouseId,
            CancellationToken cancellationToken)
    {
        var result = await warehouseService.GetByIdAsync(
            sellerId,
            warehouseId,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return Ok(result.Value!);
    }

    [HttpPost("{warehouseId:guid}/activate")]
    [ProducesResponseType(
        typeof(WarehouseResponseDto),
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
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarehouseResponseDto>>
        ActivateAsync(
            Guid sellerId,
            Guid warehouseId,
            CancellationToken cancellationToken)
    {
        var result = await warehouseService.ActivateAsync(
            sellerId,
            warehouseId,
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
                "A failed warehouse result contained no errors.");
        }

        var error = errors.First();

        if (error.Code ==
                WarehouseErrors.SellerNotFoundCode ||
            error.Code ==
                WarehouseErrors.WarehouseNotFoundCode)
        {
            return ApiProblem(
                StatusCodes.Status404NotFound,
                "Resource not found.",
                error.Description,
                error.Code);
        }

        if (error.Code ==
                WarehouseErrors.SellerUnavailableCode ||
            error.Code ==
                WarehouseErrors.DuplicateCodeCode)
        {
            return ApiProblem(
                StatusCodes.Status409Conflict,
                "Warehouse conflict.",
                error.Description,
                error.Code);
        }

        return ValidationProblemResponse(errors);
    }

    private const string GetWarehouseByIdRouteName =
    "Warehouses.GetById";

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