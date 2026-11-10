using ECommerce.Order.Application.Common;
using ECommerce.Order.Application.Contracts;

namespace ECommerce.Order.Application.Services;

public interface IOrderService
{
    Task<Result<CheckoutOrderResponse>> CheckoutAsync(
        CheckoutOrderRequest? request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Result<OrderResponse>> GetAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OrderResponse>>> GetMineAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<OrderResponse>> CancelAsync(
        Guid orderId,
        CancelOrderRequest? request,
        CancellationToken cancellationToken = default);

    Task<Result<OrderResponse>> ConfirmPaymentAsync(
        Guid orderId,
        ConfirmOrderPaymentRequest? request,
        CancellationToken cancellationToken = default);
}
