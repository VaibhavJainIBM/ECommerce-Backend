using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Warehouses.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class WarehouseRepository(
    ECommerceDbContext dbContext)
    : IWarehouseRepository
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

    public async Task<WarehouseCreateOutcome> TryCreateAsync(
        Warehouse warehouse,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(warehouse);

        dbContext.Warehouses.Add(warehouse);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);

            return WarehouseCreateOutcome.Created;
        }
        catch (DbUpdateException exception)
            when (IsDuplicateWarehouseCode(exception))
        {
            dbContext.ChangeTracker.Clear();

            return WarehouseCreateOutcome.DuplicateCode;
        }
    }

    public async Task<IReadOnlyCollection<Warehouse>>
        GetForSellerAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.Warehouses
            .AsNoTracking()
            .Where(warehouse =>
                warehouse.SellerId == sellerId)
            .OrderBy(warehouse => warehouse.Name)
            .ThenBy(warehouse => warehouse.Code)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Warehouse?> FindByIdAsync(
        Guid sellerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Warehouses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                warehouse =>
                    warehouse.SellerId == sellerId &&
                    warehouse.Id == warehouseId,
                cancellationToken);
    }

    public async Task<Warehouse?> GetTrackedAsync(
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

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static bool IsDuplicateWarehouseCode(
        DbUpdateException exception)
    {
        return exception.InnerException
                   is SqlException sqlException &&
               sqlException.Number is 2601 or 2627 &&
               sqlException.Message.Contains(
                   "IX_Warehouses_SellerId_Code",
                   StringComparison.OrdinalIgnoreCase);
    }
}