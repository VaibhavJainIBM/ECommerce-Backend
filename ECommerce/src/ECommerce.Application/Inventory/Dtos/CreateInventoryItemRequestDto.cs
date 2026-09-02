namespace ECommerce.Application.Inventory.Dtos;

public sealed class CreateInventoryItemRequestDto
{
    public Guid WarehouseId { get; init; }

    public Guid SellerListingId { get; init; }

    public int InitialQuantity { get; init; }
}