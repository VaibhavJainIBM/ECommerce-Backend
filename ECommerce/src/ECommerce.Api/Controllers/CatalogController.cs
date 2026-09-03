using ECommerce.Application.Catalog;
using ECommerce.Application.Catalog.Browsing;
using ECommerce.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/catalog/products")]
public sealed class CatalogController(ICatalogBrowsingService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedCatalogProductsResponseDto>> SearchAsync(
        [FromQuery] CatalogQueryDto? query,
        CancellationToken cancellationToken)
    {
        var result = await catalogService.SearchAsync(query, cancellationToken);
        return result.IsFailure ? ToProblem(result.Errors) : Ok(result.Value!);
    }

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<CatalogProductResponseDto>> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var result = await catalogService.GetByIdAsync(productId, cancellationToken);
        return result.IsFailure ? ToProblem(result.Errors) : Ok(result.Value!);
    }

    private ActionResult ToProblem(IReadOnlyCollection<Error> errors)
    {
        var error = errors.First();
        if (error.Code == CatalogErrors.ProductNotFoundCode)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Catalog product not found.",
                detail: error.Description,
                instance: HttpContext.Request.Path,
                extensions: new Dictionary<string, object?> { ["code"] = error.Code });
        }

        var grouped = errors.GroupBy(item => item.Code).ToDictionary(
            group => group.Key,
            group => group.Select(item => item.Description).Distinct().ToArray());
        return ValidationProblem(new ValidationProblemDetails(grouped)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid catalog query.",
            Detail = "Correct the supplied values and try again.",
            Instance = HttpContext.Request.Path
        });
    }
}
