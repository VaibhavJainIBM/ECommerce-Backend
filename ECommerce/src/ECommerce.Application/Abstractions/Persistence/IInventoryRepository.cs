using ECommerce.Application.Inventory.Models;
using ECommerce.Domain.Entities;
using ECommerce.Application.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IInventoryRepository
{
    Task<Result<InventoryItem>> UpdateQuantityAsync(Guid sellerId, Guid inventoryItemId, int quantity,
        byte[] rowVersion, bool adjustment, CancellationToken cancellationToken = default);

    Task<SellerStatus?> GetSellerStatusAsync(
        Guid sellerId,
        CancellationToken cancellationToken = default);

    Task<Warehouse?> GetWarehouseAsync(
        Guid sellerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<SellerListing?> GetSellerListingAsync(
        Guid sellerId,
        Guid sellerListingId,
        CancellationToken cancellationToken = default);

    Task<InventoryCreateOutcome> TryCreateAsync(
        InventoryItem inventoryItem,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<InventoryItem>>
        GetForSellerAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default);

    Task<InventoryItem?> FindByIdAsync(
        Guid sellerId,
        Guid inventoryItemId,
        CancellationToken cancellationToken = default);
}
