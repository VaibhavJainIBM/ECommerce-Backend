using ECommerce.Application.Warehouses.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IWarehouseRepository
{
    Task<SellerStatus?> GetSellerStatusAsync(
        Guid sellerId,
        CancellationToken cancellationToken = default);

    Task<WarehouseCreateOutcome> TryCreateAsync(
        Warehouse warehouse,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Warehouse>> GetForSellerAsync(
        Guid sellerId,
        CancellationToken cancellationToken = default);

    Task<Warehouse?> FindByIdAsync(
        Guid sellerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<Warehouse?> GetTrackedAsync(
        Guid sellerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}