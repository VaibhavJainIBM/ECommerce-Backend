namespace ECommerce.AI.Api;

public sealed record ChatHistoryItem(string Role, string Content);

public sealed record ChatRequest(
    string Message,
    IReadOnlyList<ChatHistoryItem>? History);

public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<ProductCard> Products,
    bool UsedCustomerContext);

public sealed record RecommendationRequest(
    string? Query,
    IReadOnlyList<string>? ExcludeListingIds,
    int Take = 6);

public sealed record ProductCard(
    string ListingId,
    string ProductId,
    string ProductTitle,
    string BrandName,
    string Description,
    string VariantName,
    decimal PriceAmount,
    string CurrencyCode,
    int AvailableQuantity);

public sealed record StorefrontListing(
    string ListingId,
    string SellerId,
    string SellerDisplayName,
    string ProductId,
    string ProductTitle,
    string BrandName,
    string? Description,
    string ProductVariantId,
    string VariantName,
    string VariantCode,
    string SellerSku,
    decimal PriceAmount,
    string CurrencyCode,
    int AvailableQuantity);

public sealed record StorefrontPage(
    IReadOnlyList<StorefrontListing> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CurrentUserWireResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> PlatformRoles);

public sealed record CartItemWireResponse(
    Guid ListingId,
    Guid SellerId,
    string SellerDisplayName,
    string ProductTitle,
    string VariantName,
    decimal UnitPriceAmount,
    string CurrencyCode,
    int Quantity,
    decimal LineTotal,
    long AvailableQuantity,
    bool IsAvailable);

public sealed record CartWireResponse(
    Guid? CartId,
    string? RowVersion,
    IReadOnlyList<CartItemWireResponse> Items,
    decimal TotalAmount,
    string? CurrencyCode,
    bool IsCheckoutReady);

public sealed record ShippingAddressWireResponse(
    string? Line1,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode,
    string? Line2);

public sealed record OrderItemWireResponse(
    Guid OrderItemId,
    Guid SellerId,
    string SellerDisplayName,
    Guid ListingId,
    Guid ProductVariantId,
    string ProductTitle,
    string VariantName,
    string SellerSku,
    decimal UnitPriceAmount,
    string CurrencyCode,
    int Quantity,
    decimal LineTotal,
    DateTimeOffset? ShippedAtUtc);

public sealed record OrderWireResponse(
    Guid OrderId,
    string OrderNumber,
    string Status,
    decimal TotalAmount,
    string CurrencyCode,
    string RecipientName,
    string Phone,
    ShippingAddressWireResponse ShippingAddress,
    IReadOnlyList<OrderItemWireResponse> Items,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string RowVersion,
    DateTimeOffset? PaidAtUtc,
    string? PaymentMode);

public sealed record PagedOrdersWireResponse(
    IReadOnlyList<OrderWireResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record CustomerCommerceContext(
    CurrentUserWireResponse? User,
    CartWireResponse? Cart,
    IReadOnlyList<OrderWireResponse> RecentOrders,
    OrderWireResponse? RequestedOrder,
    bool AuthenticationFailed)
{
    public bool IsAuthenticated => User is not null;
}
