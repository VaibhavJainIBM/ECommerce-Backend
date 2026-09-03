using ECommerce.Domain.ValueObjects;

namespace ECommerce.Application.Shopping;

public sealed class SetCartItemRequestDto
{
    public int Quantity { get; init; }
}

public sealed record ShippingAddressDto(
    string? Line1, string? City, string? StateOrProvince,
    string? PostalCode, string? CountryCode, string? Line2 = null);

public sealed class CheckoutRequestDto
{
    public string? CartRowVersion { get; init; }
    public decimal ExpectedTotalAmount { get; init; }
    public string? CurrencyCode { get; init; }
    public string? RecipientName { get; init; }
    public string? Phone { get; init; }
    public ShippingAddressDto? ShippingAddress { get; init; }
}

public sealed class OrderQueryDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record CartItemResponseDto(
    Guid ListingId, Guid SellerId, string SellerDisplayName,
    string ProductTitle, string VariantName, decimal UnitPriceAmount,
    string CurrencyCode, int Quantity, decimal LineTotal,
    long AvailableQuantity, bool IsAvailable);

public sealed record CartResponseDto(
    Guid? CartId, string? RowVersion, IReadOnlyList<CartItemResponseDto> Items,
    decimal TotalAmount, string? CurrencyCode, bool IsCheckoutReady);

public sealed record OrderItemResponseDto(
    Guid OrderItemId, Guid SellerId, string SellerDisplayName,
    Guid ListingId, Guid ProductVariantId, string ProductTitle,
    string VariantName, string SellerSku, decimal UnitPriceAmount,
    string CurrencyCode, int Quantity, decimal LineTotal, DateTimeOffset? ShippedAtUtc = null);

public sealed record OrderResponseDto(
    Guid OrderId, string OrderNumber, string Status, decimal TotalAmount,
    string CurrencyCode, string RecipientName, string Phone,
    ShippingAddressDto ShippingAddress, IReadOnlyList<OrderItemResponseDto> Items,
    DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc, string RowVersion,
    DateTimeOffset? PaidAtUtc = null, string? PaymentMode = null);

public sealed record CheckoutResponseDto(OrderResponseDto Order, bool Replayed);

public sealed record PagedOrdersResponseDto(
    IReadOnlyList<OrderResponseDto> Items, int Page, int PageSize, int TotalCount);

// Seller views contain only that seller's lines/subtotal, never other sellers' data.
public sealed record SellerOrderResponseDto(
    Guid OrderId, string OrderNumber, string Status, decimal SellerSubtotal,
    string CurrencyCode, string RecipientName, string Phone,
    ShippingAddressDto ShippingAddress, IReadOnlyList<OrderItemResponseDto> Items,
    DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc, string? PaymentMode = null);

public sealed record PagedSellerOrdersResponseDto(
    IReadOnlyList<SellerOrderResponseDto> Items, int Page, int PageSize, int TotalCount);

public sealed record CheckoutCommand(
    Guid CustomerId, Guid CheckoutKey, string RequestHash, byte[] CartRowVersion,
    decimal ExpectedTotalAmount, string CurrencyCode, string RecipientName,
    string Phone, Address ShippingAddress);
