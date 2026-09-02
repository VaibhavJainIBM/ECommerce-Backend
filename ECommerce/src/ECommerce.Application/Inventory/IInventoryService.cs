using ECommerce.Application.Common;
using ECommerce.Application.Inventory.Dtos;

namespace ECommerce.Application.Inventory;

public interface IInventoryService
{
    Task<Result<InventoryItemResponseDto>> CreateAsync(
        Guid sellerId,
        CreateInventoryItemRequestDto? request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<InventoryItemResponseDto>>>
        GetForSellerAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default);

    Task<Result<InventoryItemResponseDto>> GetByIdAsync(
        Guid sellerId,
        Guid inventoryItemId,
        CancellationToken cancellationToken = default);
}