using ECommerce.Product.Application.Authorization;
using ECommerce.Product.Application.Catalog;
using ECommerce.Product.Application.Catalog.Dtos;
using ECommerce.Product.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Product.Api.Controllers;

[Authorize(Roles = PlatformRoleNames.PlatformAdmin)]
[ApiController]
[Route("api/admin/catalog/products")]
public sealed class AdminCatalogController(
    IAdminCatalogService catalogService,
    IAdminCatalogQueryService queryService)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateProductResponseDto>>
        CreateProductAsync(
            [FromBody] CreateProductRequestDto? request,
            CancellationToken cancellationToken)
    {
        var result =
            await catalogService.CreateProductAsync(
                request,
                cancellationToken);

        if (result.IsFailure)
            return ToProblem(result.Errors);

        return StatusCode(
            StatusCodes.Status201Created,
            result.Value!);
    }

    [HttpPost("{productId:guid}/activate")]
    public async Task<ActionResult<CreateProductResponseDto>>
        ActivateProductAsync(
            Guid productId,
            CancellationToken cancellationToken)
    {
        var result =
            await catalogService.ActivateProductAsync(
                productId,
                cancellationToken);

        if (result.IsFailure)
            return ToProblem(result.Errors);

        return Ok(result.Value!);
    }

    [HttpGet]
    public async Task<
        ActionResult<PagedAdminCatalogProductsResponseDto>>
        GetProductsAsync(
            [FromQuery] AdminCatalogQueryDto? query,
            CancellationToken cancellationToken)
    {
        var result =
            await queryService.GetProductsAsync(
                query,
                cancellationToken);

        if (result.IsFailure)
        {
            var grouped =
                result.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(error =>
                                error.Description)
                            .Distinct()
                            .ToArray());

            return ValidationProblem(
                new ValidationProblemDetails(grouped)
                {
                    Status =
                        StatusCodes.Status400BadRequest,
                    Title =
                        "The admin catalog query is invalid."
                });
        }

        return Ok(result.Value!);
    }

    private ActionResult ToProblem(
        IReadOnlyCollection<Error> errors)
    {
        var error = errors.First();

        if (error.Code ==
            CatalogErrors.ProductNotFoundCode)
        {
            return Problem(
                statusCode: 404,
                title: "Catalog product not found.",
                detail: error.Description);
        }

        if (error.Code ==
                CatalogErrors.GtinConflictCode ||
            error.Code ==
                CatalogErrors.ActivationConflictCode)
        {
            return Problem(
                statusCode: 409,
                title: "Catalog conflict.",
                detail: error.Description);
        }

        var grouped =
            errors
                .GroupBy(x => x.Code)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => y.Description)
                        .Distinct()
                        .ToArray());

        return ValidationProblem(
            new ValidationProblemDetails(grouped)
            {
                Status = 400,
                Title =
                    "One or more validation errors occurred."
            });
    }
}