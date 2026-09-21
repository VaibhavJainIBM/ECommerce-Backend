using ECommerce.Application.Administration;
using ECommerce.Application.Authorization;
using ECommerce.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize(Roles = PlatformRoleNames.PlatformAdmin)]
[ApiController]
[Route("api/admin")]
public sealed class AdminQueriesController(
    IAdminQueryService queryService)
    : ControllerBase
{
    [HttpGet("sellers")]
    public async Task<ActionResult<PagedAdminSellersResponseDto>>
        GetSellersAsync(
            [FromQuery] AdminQueryDto query,
            CancellationToken cancellationToken)
    {
        var result = await queryService.GetSellersAsync(
            query,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value!)
            : ToProblem(result.Errors);
    }

    [HttpGet("listings")]
    public async Task<ActionResult<PagedAdminListingsResponseDto>>
        GetListingsAsync(
            [FromQuery] AdminQueryDto query,
            CancellationToken cancellationToken)
    {
        var result = await queryService.GetListingsAsync(
            query,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value!)
            : ToProblem(result.Errors);
    }

    [HttpGet("catalog/products")]
    public async Task<ActionResult<PagedAdminCatalogProductsResponseDto>>
        GetCatalogProductsAsync(
            [FromQuery] AdminQueryDto query,
            CancellationToken cancellationToken)
    {
        var result = await queryService.GetCatalogProductsAsync(
            query,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value!)
            : ToProblem(result.Errors);
    }

    private ActionResult ToProblem(
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

        var problem = new ValidationProblemDetails(
            groupedErrors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The admin query is invalid.",
            Detail = "Correct the query and try again.",
            Instance = HttpContext.Request.Path
        };

        problem.Extensions["traceId"] =
            HttpContext.TraceIdentifier;

        return ValidationProblem(problem);
    }
}
