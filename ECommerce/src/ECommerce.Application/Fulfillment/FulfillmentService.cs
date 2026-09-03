using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Application.Shopping;

namespace ECommerce.Application.Fulfillment;

public sealed class FulfillmentService(IFulfillmentRepository repository, ICurrentUser currentUser)
    : IFulfillmentService
{
    public Task<Result<SellerOrderResponseDto>> ShipSellerOrderAsync(
        Guid sellerId, Guid orderId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid actorId || actorId == Guid.Empty)
            return Task.FromResult(Result<SellerOrderResponseDto>.Failure(ShoppingErrors.Unauthenticated));
        if (sellerId == Guid.Empty || orderId == Guid.Empty)
            return Task.FromResult(Result<SellerOrderResponseDto>.Failure(
                ShoppingErrors.Validation("Seller ID and order ID are required.")));
        return repository.ShipSellerOrderAsync(actorId, sellerId, orderId, cancellationToken);
    }
}
