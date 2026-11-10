using ECommerce.Order.Application.Abstractions;
using ECommerce.Order.Application.Common;
using ECommerce.Order.Application.Contracts;
using ECommerce.Order.Domain.Entities;

namespace ECommerce.Order.Application.Services;

public sealed class OrderService(
    IOrderRepository repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IOrderService
{
    private static readonly TimeSpan PaymentWindow =
        TimeSpan.FromMinutes(30);

    public async Task<Result<CheckoutOrderResponse>> CheckoutAsync(
        CheckoutOrderRequest? request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.UserId;

        if (!customerId.HasValue)
            return Failure<CheckoutOrderResponse>(OrderErrors.Unauthenticated);

        if (!Guid.TryParse(idempotencyKey, out var requestKey) ||
            requestKey == Guid.Empty)
        {
            return Failure<CheckoutOrderResponse>(
                OrderErrors.Validation("A GUID Idempotency-Key header is required."));
        }

        if (request is null ||
            request.ShippingAddress is null ||
            request.Items is null ||
            request.Items.Count == 0)
        {
            return Failure<CheckoutOrderResponse>(
                OrderErrors.Validation("Shipping address and at least one item are required."));
        }

        var existing =
            await repository.GetByIdempotencyKeyAsync(
                customerId.Value,
                requestKey,
                cancellationToken);

        if (existing is not null)
        {
            return Result<CheckoutOrderResponse>.Success(
                new CheckoutOrderResponse(Map(existing), true));
        }

        try
        {
            var address = new ShippingAddressSnapshot(
                request.ShippingAddress.FullName,
                request.ShippingAddress.Line1,
                request.ShippingAddress.Line2,
                request.ShippingAddress.City,
                request.ShippingAddress.State,
                request.ShippingAddress.PostalCode,
                request.ShippingAddress.CountryCode);

            var lines = request.Items
                .Select(x => new OrderLineSnapshot(
                    x.SellerId,
                    x.SellerListingId,
                    x.ProductVariantId,
                    x.SellerSku,
                    x.ProductTitle,
                    x.VariantName,
                    x.Quantity,
                    x.UnitPrice))
                .ToArray();

            var order = CustomerOrder.Create(
                customerId.Value,
                requestKey,
                request.CurrencyCode,
                address,
                lines,
                timeProvider.GetUtcNow(),
                PaymentWindow);

            await repository.AddAsync(order, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            return Result<CheckoutOrderResponse>.Success(
                new CheckoutOrderResponse(Map(order), false));
        }
        catch (ArgumentException exception)
        {
            return Failure<CheckoutOrderResponse>(
                OrderErrors.Validation(exception.Message));
        }
    }

    public async Task<Result<OrderResponse>> GetAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.UserId;

        if (!customerId.HasValue)
            return Failure<OrderResponse>(OrderErrors.Unauthenticated);

        var order = await repository.GetByIdAsync(
            orderId,
            customerId.Value,
            false,
            cancellationToken);

        return order is null
            ? Failure<OrderResponse>(OrderErrors.NotFound("The order was not found."))
            : Result<OrderResponse>.Success(Map(order));
    }

    public async Task<Result<IReadOnlyList<OrderResponse>>> GetMineAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.UserId;

        if (!customerId.HasValue)
            return Failure<IReadOnlyList<OrderResponse>>(OrderErrors.Unauthenticated);

        if (page < 1 || pageSize is < 1 or > 100)
        {
            return Failure<IReadOnlyList<OrderResponse>>(
                OrderErrors.Validation("Page must be positive and pageSize must be between 1 and 100."));
        }

        var orders = await repository.GetForCustomerAsync(
            customerId.Value,
            page,
            pageSize,
            cancellationToken);

        return Result<IReadOnlyList<OrderResponse>>.Success(
            orders.Select(Map).ToArray());
    }

    public async Task<Result<OrderResponse>> CancelAsync(
        Guid orderId,
        CancelOrderRequest? request,
        CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.UserId;

        if (!customerId.HasValue)
            return Failure<OrderResponse>(OrderErrors.Unauthenticated);

        var order = await repository.GetByIdAsync(
            orderId,
            customerId.Value,
            true,
            cancellationToken);

        if (order is null)
            return Failure<OrderResponse>(OrderErrors.NotFound("The order was not found."));

        try
        {
            order.Cancel(request?.Reason, timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(cancellationToken);
            return Result<OrderResponse>.Success(Map(order));
        }
        catch (InvalidOperationException exception)
        {
            return Failure<OrderResponse>(OrderErrors.Conflict(exception.Message));
        }
    }

    public async Task<Result<OrderResponse>> ConfirmPaymentAsync(
        Guid orderId,
        ConfirmOrderPaymentRequest? request,
        CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.UserId;

        if (!customerId.HasValue)
            return Failure<OrderResponse>(OrderErrors.Unauthenticated);

        if (request is null)
        {
            return Failure<OrderResponse>(
                OrderErrors.Validation("Payment confirmation details are required."));
        }

        var order = await repository.GetByIdAsync(
            orderId,
            customerId.Value,
            true,
            cancellationToken);

        if (order is null)
            return Failure<OrderResponse>(OrderErrors.NotFound("The order was not found."));

        try
        {
            order.ConfirmPayment(
                request.PaymentId,
                request.Amount,
                request.CurrencyCode,
                timeProvider.GetUtcNow());

            await repository.SaveChangesAsync(cancellationToken);
            return Result<OrderResponse>.Success(Map(order));
        }
        catch (InvalidOperationException exception)
        {
            return Failure<OrderResponse>(OrderErrors.Conflict(exception.Message));
        }
    }

    private static Result<T> Failure<T>(Error error) =>
        Result<T>.Failure(error);

    private static OrderResponse Map(CustomerOrder order) =>
        new(
            order.Id,
            order.Status.ToString(),
            order.TotalAmount,
            order.CurrencyCode,
            order.CreatedAtUtc,
            order.ExpiresAtUtc,
            order.PaidAtUtc,
            order.PaymentId,
            order.CancelledAtUtc,
            order.CancellationReason,
            new ShippingAddressResponse(
                order.ShippingFullName,
                order.ShippingLine1,
                order.ShippingLine2,
                order.ShippingCity,
                order.ShippingState,
                order.ShippingPostalCode,
                order.ShippingCountryCode),
            order.Items
                .Select(x => new OrderItemResponse(
                    x.Id,
                    x.SellerId,
                    x.SellerListingId,
                    x.ProductVariantId,
                    x.SellerSku,
                    x.ProductTitle,
                    x.VariantName,
                    x.Quantity,
                    x.UnitPrice,
                    x.LineTotal))
                .ToArray());
}
