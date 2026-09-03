using ECommerce.Application.Common;
using ECommerce.Application.Shopping;

namespace ECommerce.Application.Abstractions.Persistence;

// Atomic commands own their database transaction; domain objects enforce invariants.
public interface IShoppingRepository
{
    Task<Result<CartResponseDto>> GetCartAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<Result<CartResponseDto>> SetCartItemAsync(Guid customerId, Guid listingId, int quantity, CancellationToken cancellationToken = default);
    Task<Result<CartResponseDto>> RemoveCartItemAsync(Guid customerId, Guid listingId, CancellationToken cancellationToken = default);
    Task<Result<CartResponseDto>> ClearCartAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<Result<CheckoutResponseDto>> CheckoutAsync(CheckoutCommand command, CancellationToken cancellationToken = default);
    Task<Result<PagedOrdersResponseDto>> GetOrdersAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<OrderResponseDto>> GetOrderAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default);
    Task<Result<OrderResponseDto>> CancelOrderAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default);
    Task<Result<PagedSellerOrdersResponseDto>> GetSellerOrdersAsync(Guid sellerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> ExpireOrdersAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken = default);
}
