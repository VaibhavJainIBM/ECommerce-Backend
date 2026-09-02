using ECommerce.Application.Common;
using ECommerce.Application.Warehouses.Dtos;

namespace ECommerce.Application.Warehouses;

public interface IWarehouseService
{
    Task<Result<WarehouseResponseDto>> CreateAsync(
        Guid sellerId,
        CreateWarehouseRequestDto? request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<WarehouseResponseDto>>>
        GetForSellerAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default);

    Task<Result<WarehouseResponseDto>> GetByIdAsync(
        Guid sellerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<Result<WarehouseResponseDto>> ActivateAsync(
        Guid sellerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);
}