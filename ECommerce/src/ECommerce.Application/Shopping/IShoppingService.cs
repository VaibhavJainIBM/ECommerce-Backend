using ECommerce.Application.Common;

namespace ECommerce.Application.Shopping;

public interface IShoppingService
{
    Task<Result<CartResponseDto>> GetCartAsync(CancellationToken cancellationToken = default);
    Task<Result<CartResponseDto>> SetCartItemAsync(Guid listingId, SetCartItemRequestDto? request, CancellationToken cancellationToken = default);
    Task<Result<CartResponseDto>> RemoveCartItemAsync(Guid listingId, CancellationToken cancellationToken = default);
    Task<Result<CartResponseDto>> ClearCartAsync(CancellationToken cancellationToken = default);
    Task<Result<CheckoutResponseDto>> CheckoutAsync(string? idempotencyKey, CheckoutRequestDto? request, CancellationToken cancellationToken = default);
    Task<Result<PagedOrdersResponseDto>> GetOrdersAsync(OrderQueryDto? query, CancellationToken cancellationToken = default);
    Task<Result<OrderResponseDto>> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Result<OrderResponseDto>> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Result<PagedSellerOrdersResponseDto>> GetSellerOrdersAsync(Guid sellerId, OrderQueryDto? query, CancellationToken cancellationToken = default);
}
