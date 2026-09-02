namespace ECommerce.Application.Inventory.Dtos;

public sealed record InventoryItemResponseDto(
    Guid InventoryItemId,
    Guid SellerId,
    Guid WarehouseId,
    string WarehouseName,
    string WarehouseCode,
    Guid SellerListingId,
    string SellerSku,
    Guid ProductVariantId,
    int OnHandQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);