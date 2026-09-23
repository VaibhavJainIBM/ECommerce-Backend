using ECommerce.Product.Application.Contracts;
using ECommerce.Product.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Product.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(
    IProductService productService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyCollection<ProductResponse>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var products =
            await productService.GetAllAsync(
                cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>>
        GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
    {
        var product =
            await productService.GetByIdAsync(
                id,
                cancellationToken);

        return product is null
            ? NotFound()
            : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>>
        CreateAsync(
            [FromBody] CreateProductRequest request,
            CancellationToken cancellationToken)
    {
        var product =
            await productService.CreateAsync(
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new
            {
                id = product.Id
            },
            product);
    }

    [HttpPost("{productId:guid}/variants")]
    public async Task<ActionResult<ProductResponse>>
        AddVariantAsync(
            Guid productId,
            [FromBody] CreateVariantRequest request,
            CancellationToken cancellationToken)
    {
        var product =
            await productService.AddVariantAsync(
                productId,
                request,
                cancellationToken);

        return product is null
            ? NotFound()
            : Ok(product);
    }
}