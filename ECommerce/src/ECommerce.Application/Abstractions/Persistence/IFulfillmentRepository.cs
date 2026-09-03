using ECommerce.Application.Common;
using ECommerce.Application.Shopping;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IFulfillmentRepository
{
    Task<Result<SellerOrderResponseDto>> ShipSellerOrderAsync(
        Guid actorId, Guid sellerId, Guid orderId, CancellationToken cancellationToken = default);
}
