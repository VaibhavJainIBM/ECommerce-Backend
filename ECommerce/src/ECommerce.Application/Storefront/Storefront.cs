namespace ECommerce.Application.Storefront;

public sealed class StorefrontQueryDto
{
    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed record StorefrontListingResponseDto(
    Guid ListingId,
    Guid SellerId,
    string SellerDisplayName,
    Guid ProductId,
    string ProductTitle,
    string BrandName,
    string? Description,
    Guid ProductVariantId,
    string VariantName,
    string VariantCode,
    string SellerSku,
    decimal PriceAmount,
    string CurrencyCode,
    long AvailableQuantity);

public sealed record PagedStorefrontListingsResponseDto(
    IReadOnlyCollection<StorefrontListingResponseDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record StorefrontListingReadModel(
    Guid ListingId,
    Guid SellerId,
    string SellerDisplayName,
    Guid ProductId,
    string ProductTitle,
    string BrandName,
    string? Description,
    Guid ProductVariantId,
    string VariantName,
    string VariantCode,
    string SellerSku,
    decimal PriceAmount,
    string CurrencyCode,
    long AvailableQuantity);

public sealed record StorefrontListingPage(
    IReadOnlyCollection<StorefrontListingReadModel> Items,
    int TotalCount);
