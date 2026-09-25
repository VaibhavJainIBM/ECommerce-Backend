namespace ECommerce.Order.Application.Contracts;

public sealed record ShippingAddressRequest(
    string FullName,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string CountryCode);

public sealed record CheckoutOrderLineRequest(
    Guid SellerId,
    Guid SellerListingId,
    Guid ProductVariantId,
    string SellerSku,
    string ProductTitle,
    string VariantName,
    int Quantity,
    decimal UnitPrice);

public sealed record CheckoutOrderRequest(
    string CurrencyCode,
    ShippingAddressRequest ShippingAddress,
    IReadOnlyCollection<CheckoutOrderLineRequest> Items);

public sealed record CancelOrderRequest(
    string? Reason);

public sealed record ConfirmOrderPaymentRequest(
    Guid PaymentId,
    decimal Amount,
    string CurrencyCode);

public sealed record OrderItemResponse(
    Guid OrderItemId,
    Guid SellerId,
    Guid SellerListingId,
    Guid ProductVariantId,
    string SellerSku,
    string ProductTitle,
    string VariantName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record ShippingAddressResponse(
    string FullName,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string CountryCode);

public sealed record OrderResponse(
    Guid OrderId,
    string Status,
    decimal TotalAmount,
    string CurrencyCode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? PaidAtUtc,
    Guid? PaymentId,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason,
    ShippingAddressResponse ShippingAddress,
    IReadOnlyCollection<OrderItemResponse> Items);

public sealed record CheckoutOrderResponse(
    OrderResponse Order,
    bool Replayed);
