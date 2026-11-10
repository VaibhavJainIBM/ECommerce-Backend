using ECommerce.Product.Application.Catalog;
using ECommerce.Product.Application.Catalog.Browsing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Product.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/catalog/products")]
public sealed class CatalogController(
    ICatalogBrowsingService catalogService)
    : ControllerBase
{
    [HttpGet]
    public async Task<
        ActionResult<PagedCatalogProductsResponseDto>>
        SearchAsync(
            [FromQuery] CatalogQueryDto? query,
            CancellationToken cancellationToken)
    {
        var result =
            await catalogService.SearchAsync(
                query,
                cancellationToken);

        if (result.IsFailure)
            return BadRequest(result.Errors);

        return Ok(result.Value!);
    }

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<CatalogProductResponseDto>>
        GetByIdAsync(
            Guid productId,
            CancellationToken cancellationToken)
    {
        var result =
            await catalogService.GetByIdAsync(
                productId,
                cancellationToken);

        if (result.IsFailure)
            return NotFound();

        return Ok(result.Value!);
    }
}