namespace ECommerce.Application.Inventory.Dtos;
public sealed record UpdateInventoryQuantityRequestDto(int Quantity, string? RowVersion);
