namespace ECommerce.Application.Storefront;

public sealed class StorefrontQueryDto
{
    public string? Search { get; init; }

    public string? Brand { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public string? Sort { get; init; } =
        StorefrontSortNames.NameAscending;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public static class StorefrontSortNames
{
    public const string NameAscending = "name_asc";
    public const string NameDescending = "name_desc";
    public const string PriceAscending = "price_asc";
    public const string PriceDescending = "price_desc";
}

public enum StorefrontSort
{
    NameAscending,
    NameDescending,
    PriceAscending,
    PriceDescending
}

public sealed record StorefrontSearchCriteria(
    string? Search,
    string? Brand,
    decimal? MinPrice,
    decimal? MaxPrice,
    StorefrontSort Sort,
    int Skip,
    int Take);

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
