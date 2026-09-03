using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Application.Inventory.Dtos;
using ECommerce.Application.Inventory.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Inventory;

public sealed class InventoryService(
    IInventoryRepository repository)
    : IInventoryService
{
    public async Task<Result<InventoryItemResponseDto>> UpdateQuantityAsync(Guid sellerId, Guid inventoryItemId,
        UpdateInventoryQuantityRequestDto? request, bool adjustment, CancellationToken cancellationToken = default)
    {
        if (sellerId == Guid.Empty || inventoryItemId == Guid.Empty || request is null ||
            request.Quantity < 0 || (!adjustment && request.Quantity == 0))
            return Result<InventoryItemResponseDto>.Failure(new Error("inventory.quantity_invalid",
                adjustment ? "Provide an on-hand quantity of zero or more." : "Provide a positive quantity to receive."));
        byte[] rowVersion;
        try { rowVersion = Convert.FromBase64String(request.RowVersion ?? ""); }
        catch (FormatException) { return Result<InventoryItemResponseDto>.Failure(new Error("inventory.row_version_invalid", "Provide the current inventory rowVersion.")); }
        if (rowVersion.Length != 8)
            return Result<InventoryItemResponseDto>.Failure(new Error("inventory.row_version_invalid", "Provide the current inventory rowVersion."));
        var result = await repository.UpdateQuantityAsync(sellerId, inventoryItemId, request.Quantity, rowVersion, adjustment, cancellationToken);
        return result.IsSuccess ? Result<InventoryItemResponseDto>.Success(Map(result.Value!)) :
            Result<InventoryItemResponseDto>.Failure(result.Errors);
    }

    public async Task<Result<InventoryItemResponseDto>>
        CreateAsync(
            Guid sellerId,
            CreateInventoryItemRequestDto? request,
            CancellationToken cancellationToken = default)
    {
        var errors = ValidateCreate(sellerId, request);

        if (errors.Count > 0)
        {
            return Result<InventoryItemResponseDto>.Failure(
                errors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sellerStatus =
            await repository.GetSellerStatusAsync(
                sellerId,
                cancellationToken);

        if (!sellerStatus.HasValue)
        {
            return Result<InventoryItemResponseDto>.Failure(
                InventoryErrors.SellerNotFound);
        }

        if (sellerStatus.Value != SellerStatus.Active)
        {
            return Result<InventoryItemResponseDto>.Failure(
                InventoryErrors.SellerUnavailable(
                    sellerStatus.Value.ToString()));
        }

        var warehouse = await repository.GetWarehouseAsync(
            sellerId,
            request!.WarehouseId,
            cancellationToken);

        if (warehouse is null)
        {
            return Result<InventoryItemResponseDto>.Failure(
                InventoryErrors.WarehouseNotFound);
        }

        if (warehouse.Status != WarehouseStatus.Active)
        {
            return Result<InventoryItemResponseDto>.Failure(
                InventoryErrors.WarehouseUnavailable(
                    warehouse.Status.ToString()));
        }

        var listing =
            await repository.GetSellerListingAsync(
                sellerId,
                request.SellerListingId,
                cancellationToken);

        if (listing is null)
        {
            return Result<InventoryItemResponseDto>.Failure(
                InventoryErrors.ListingNotFound);
        }

        if (listing.Status == SellerListingStatus.Archived)
        {
            return Result<InventoryItemResponseDto>.Failure(
                InventoryErrors.ListingUnavailable(
                    listing.Status.ToString()));
        }

        var inventoryItem = new InventoryItem(
            warehouse,
            listing);

        if (request.InitialQuantity > 0)
        {
            inventoryItem.Receive(
                request.InitialQuantity);
        }

        var outcome = await repository.TryCreateAsync(
            inventoryItem,
            cancellationToken);

        if (outcome == InventoryCreateOutcome.NotAuthorized)
            return Result<InventoryItemResponseDto>.Failure(InventoryErrors.WarehouseNotFound);

        if (outcome ==
            InventoryCreateOutcome.DuplicateWarehouseListing)
        {
            return Result<InventoryItemResponseDto>.Failure(
                InventoryErrors.DuplicateInventory);
        }

        return Result<InventoryItemResponseDto>.Success(
            Map(inventoryItem));
    }

    public async Task<
        Result<IReadOnlyCollection<InventoryItemResponseDto>>>
        GetForSellerAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default)
    {
        if (sellerId == Guid.Empty)
        {
            return Result<
                IReadOnlyCollection<InventoryItemResponseDto>>
                .Failure(
                    InventoryErrors.SellerIdRequired);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sellerStatus =
            await repository.GetSellerStatusAsync(
                sellerId,
                cancellationToken);

        if (!sellerStatus.HasValue)
        {
            return Result<
                IReadOnlyCollection<InventoryItemResponseDto>>
                .Failure(
                    InventoryErrors.SellerNotFound);
        }

        var inventoryItems =
            await repository.GetForSellerAsync(
                sellerId,
                cancellationToken);

        IReadOnlyCollection<InventoryItemResponseDto> response =
            inventoryItems
                .Select(Map)
                .ToArray();

        return Result<
            IReadOnlyCollection<InventoryItemResponseDto>>
            .Success(response);
    }

    public async Task<Result<InventoryItemResponseDto>>
        GetByIdAsync(
            Guid sellerId,
            Guid inventoryItemId,
            CancellationToken cancellationToken = default)
    {
        var errors = ValidateIds(
            sellerId,
            inventoryItemId);

        if (errors.Count > 0)
        {
            return Result<InventoryItemResponseDto>.Failure(
                errors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var inventoryItem = await repository.FindByIdAsync(
            sellerId,
            inventoryItemId,
            cancellationToken);

        if (inventoryItem is null)
        {
            return Result<InventoryItemResponseDto>.Failure(
                InventoryErrors.InventoryItemNotFound(
                    inventoryItemId));
        }

        return Result<InventoryItemResponseDto>.Success(
            Map(inventoryItem));
    }

    private static List<Error> ValidateCreate(
        Guid sellerId,
        CreateInventoryItemRequestDto? request)
    {
        var errors = new List<Error>();

        if (sellerId == Guid.Empty)
        {
            errors.Add(
                InventoryErrors.SellerIdRequired);
        }

        if (request is null)
        {
            errors.Add(
                InventoryErrors.RequestRequired);

            return errors;
        }

        if (request.WarehouseId == Guid.Empty)
        {
            errors.Add(
                InventoryErrors.WarehouseIdRequired);
        }

        if (request.SellerListingId == Guid.Empty)
        {
            errors.Add(
                InventoryErrors.ListingIdRequired);
        }

        if (request.InitialQuantity < 0)
        {
            errors.Add(
                InventoryErrors.InitialQuantityInvalid);
        }

        return errors;
    }

    private static List<Error> ValidateIds(
        Guid sellerId,
        Guid inventoryItemId)
    {
        var errors = new List<Error>();

        if (sellerId == Guid.Empty)
        {
            errors.Add(
                InventoryErrors.SellerIdRequired);
        }

        if (inventoryItemId == Guid.Empty)
        {
            errors.Add(
                InventoryErrors.InventoryItemIdRequired);
        }

        return errors;
    }

    private static InventoryItemResponseDto Map(
        InventoryItem inventoryItem)
    {
        return new InventoryItemResponseDto(
            inventoryItem.Id,
            inventoryItem.SellerId,
            inventoryItem.WarehouseId,
            inventoryItem.Warehouse.Name,
            inventoryItem.Warehouse.Code,
            inventoryItem.SellerListingId,
            inventoryItem.SellerListing.SellerSku,
            inventoryItem.SellerListing.ProductVariantId,
            inventoryItem.OnHandQuantity,
            inventoryItem.ReservedQuantity,
            inventoryItem.AvailableQuantity,
            Convert.ToBase64String(
                inventoryItem.RowVersion),
            inventoryItem.CreatedAtUtc,
            inventoryItem.UpdatedAtUtc);
    }
}
