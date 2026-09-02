using ECommerce.Application.Common;
using ECommerce.Application.Inventory;
using ECommerce.Application.Inventory.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/sellers/{sellerId:guid}/inventory")]
public sealed class InventoryController(
    IInventoryService inventoryService)
    : ControllerBase
{
    // GET: api/sellers/{sellerId}/inventory
    [HttpGet]
    public async Task<IActionResult> GetForSeller(
        Guid sellerId,
        CancellationToken cancellationToken)
    {
        var result =
            await inventoryService.GetForSellerAsync(
                sellerId,
                cancellationToken);

        if (!result.IsSuccess)
        {
            return MapErrors(result.Errors);
        }

        return Ok(result.Value);
    }


    // GET: api/sellers/{sellerId}/inventory/{inventoryItemId}
    [HttpGet("{inventoryItemId:guid}")]
    public async Task<IActionResult> GetById(
        Guid sellerId,
        Guid inventoryItemId,
        CancellationToken cancellationToken)
    {
        var result =
            await inventoryService.GetByIdAsync(
                sellerId,
                inventoryItemId,
                cancellationToken);

        if (!result.IsSuccess)
        {
            return MapErrors(result.Errors);
        }

        return Ok(result.Value);
    }


    // POST: api/sellers/{sellerId}/inventory
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid sellerId,
        [FromBody] CreateInventoryItemRequestDto? request,
        CancellationToken cancellationToken)
    {
        var result =
            await inventoryService.CreateAsync(
                sellerId,
                request,
                cancellationToken);

        if (!result.IsSuccess)
        {
            return MapErrors(result.Errors);
        }

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                sellerId,
                inventoryItemId =
                    result.Value!.InventoryItemId
            },
            result.Value);
    }


    private IActionResult MapErrors(
        IReadOnlyCollection<Error> errors)
    {
        // 404 - requested resource does not exist
        if (errors.Any(error =>
                error.Code ==
                    InventoryErrors.SellerNotFoundCode ||
                error.Code ==
                    InventoryErrors.WarehouseNotFoundCode ||
                error.Code ==
                    InventoryErrors.ListingNotFoundCode ||
                error.Code ==
                    InventoryErrors.InventoryNotFoundCode))
        {
            return NotFound(new
            {
                errors
            });
        }

        // 409 - resource exists, but request conflicts
        // with the current state
        if (errors.Any(error =>
                error.Code ==
                    InventoryErrors.DuplicateInventoryCode ||
                error.Code ==
                    InventoryErrors.SellerUnavailableCode ||
                error.Code ==
                    InventoryErrors.WarehouseUnavailableCode ||
                error.Code ==
                    InventoryErrors.ListingUnavailableCode))
        {
            return Conflict(new
            {
                errors
            });
        }

        // Validation errors
        return BadRequest(new
        {
            errors
        });
    }
}