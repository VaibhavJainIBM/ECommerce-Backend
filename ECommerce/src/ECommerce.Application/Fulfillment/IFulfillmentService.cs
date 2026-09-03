using ECommerce.Application.Common;
using ECommerce.Application.Shopping;

namespace ECommerce.Application.Fulfillment;

public interface IFulfillmentService
{
    Task<Result<SellerOrderResponseDto>> ShipSellerOrderAsync(
        Guid sellerId, Guid orderId, CancellationToken cancellationToken = default);
}
