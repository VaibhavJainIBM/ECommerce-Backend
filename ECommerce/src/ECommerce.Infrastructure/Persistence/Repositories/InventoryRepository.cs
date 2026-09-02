using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Inventory.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository(
    ECommerceDbContext dbContext)
    : IInventoryRepository
{
    public async Task<SellerStatus?> GetSellerStatusAsync(
        Guid sellerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Sellers
            .AsNoTracking()
            .Where(seller => seller.Id == sellerId)
            .Select(seller => (SellerStatus?)seller.Status)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Warehouse?> GetWarehouseAsync(
        Guid sellerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Warehouses
            .SingleOrDefaultAsync(
                warehouse =>
                    warehouse.SellerId == sellerId &&
                    warehouse.Id == warehouseId,
                cancellationToken);
    }

    public async Task<SellerListing?> GetSellerListingAsync(
        Guid sellerId,
        Guid sellerListingId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SellerListings
            .SingleOrDefaultAsync(
                listing =>
                    listing.SellerId == sellerId &&
                    listing.Id == sellerListingId,
                cancellationToken);
    }

    public async Task<InventoryCreateOutcome> TryCreateAsync(
        InventoryItem inventoryItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inventoryItem);

        dbContext.InventoryItems.Add(inventoryItem);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);

            return InventoryCreateOutcome.Created;
        }
        catch (DbUpdateException exception)
            when (IsDuplicateWarehouseListing(exception))
        {
            dbContext.ChangeTracker.Clear();

            return InventoryCreateOutcome
                .DuplicateWarehouseListing;
        }
    }

    public async Task<IReadOnlyCollection<InventoryItem>>
        GetForSellerAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.InventoryItems
            .AsNoTracking()
            .Include(inventory =>
                inventory.Warehouse)
            .Include(inventory =>
                inventory.SellerListing)
            .Where(inventory =>
                inventory.SellerId == sellerId)
            .OrderBy(inventory =>
                inventory.Warehouse.Code)
            .ThenBy(inventory =>
                inventory.SellerListing.SellerSku)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<InventoryItem?> FindByIdAsync(
        Guid sellerId,
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.InventoryItems
            .AsNoTracking()
            .Include(inventory =>
                inventory.Warehouse)
            .Include(inventory =>
                inventory.SellerListing)
            .SingleOrDefaultAsync(
                inventory =>
                    inventory.SellerId == sellerId &&
                    inventory.Id == inventoryItemId,
                cancellationToken);
    }

    private static bool IsDuplicateWarehouseListing(
        DbUpdateException exception)
    {
        return exception.InnerException
                   is SqlException sqlException &&
               sqlException.Number is 2601 or 2627 &&
               sqlException.Message.Contains(
                   "IX_InventoryItems_SellerId_" +
                   "WarehouseId_SellerListingId",
                   StringComparison.OrdinalIgnoreCase);
    }
}