using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Inventory.Models;
using ECommerce.Application.Common;
using ECommerce.Application.Inventory;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository(
    ECommerceDbContext dbContext,
    SellerDataScope dataScope)
    : IInventoryRepository
{
    public async Task<Result<InventoryItem>> UpdateQuantityAsync(Guid sellerId, Guid inventoryItemId,
        int quantity, byte[] rowVersion, bool adjustment, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (!await AcquireMutationLockAsync(sellerId, cancellationToken))
            return Result<InventoryItem>.Failure(new Error("inventory.stock_conflict", "Seller access is changing. Retry."));
        var item = await dataScope.Inventory(sellerId)
            .Include(i => i.Warehouse).Include(i => i.SellerListing).ThenInclude(l => l.Seller)
            .SingleOrDefaultAsync(i => i.Id == inventoryItemId, cancellationToken);
        if (item is null) return Result<InventoryItem>.Failure(InventoryErrors.InventoryItemNotFound(inventoryItemId));
        if (item.SellerListing.Seller.Status != SellerStatus.Active)
            return Result<InventoryItem>.Failure(InventoryErrors.SellerUnavailable(item.SellerListing.Seller.Status.ToString()));
        if (item.Warehouse.Status != WarehouseStatus.Active)
            return Result<InventoryItem>.Failure(InventoryErrors.WarehouseUnavailable(item.Warehouse.Status.ToString()));
        if (item.SellerListing.Status == SellerListingStatus.Archived)
            return Result<InventoryItem>.Failure(InventoryErrors.ListingUnavailable(item.SellerListing.Status.ToString()));
        if (!item.RowVersion.SequenceEqual(rowVersion))
            return Result<InventoryItem>.Failure(new Error("inventory.stock_conflict", "Inventory changed. Get its latest rowVersion and retry."));
        try
        {
            if (adjustment) item.AdjustOnHand(quantity); else item.Receive(quantity);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<InventoryItem>.Success(item);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return Result<InventoryItem>.Failure(new Error("inventory.stock_conflict", "Inventory changed. Refresh and retry."));
        }
        catch (Exception ex) when (ex is OverflowException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            dbContext.ChangeTracker.Clear();
            return Result<InventoryItem>.Failure(new Error("inventory.quantity_invalid",
                "Quantity must fit an integer and adjusted on-hand stock cannot be below reserved stock."));
        }
    }

    private async Task<bool> AcquireMutationLockAsync(Guid sellerId, CancellationToken ct)
    {
        var output = new SqlParameter("@lockResult", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
        var resource = new SqlParameter("@resource", System.Data.SqlDbType.NVarChar, 255)
            { Value = "ecommerce:seller-team:" + sellerId.ToString("N") };
        await dbContext.Database.ExecuteSqlRawAsync(
            "EXEC @lockResult = sys.sp_getapplock @Resource=@resource, @LockMode=N'Shared', @LockOwner=N'Transaction', @LockTimeout=5000;",
            [output, resource], ct);
        return output.Value is int status && status >= 0;
    }

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
        return await dataScope.Warehouses(sellerId)
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (!await AcquireMutationLockAsync(inventoryItem.SellerId, cancellationToken) ||
            !await dataScope.Warehouses(inventoryItem.SellerId).AnyAsync(w => w.Id == inventoryItem.WarehouseId, cancellationToken))
            return InventoryCreateOutcome.NotAuthorized;

        dbContext.InventoryItems.Add(inventoryItem);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

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
        return await dataScope.Inventory(sellerId)
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
        return await dataScope.Inventory(sellerId)
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
