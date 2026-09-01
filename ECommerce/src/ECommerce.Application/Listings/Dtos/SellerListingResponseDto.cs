namespace ECommerce.Application.Listings.Dtos;

public sealed record SellerListingResponseDto(
    Guid ListingId,
    Guid SellerId,
    Guid ProductId,
    string ProductTitle,
    string BrandName,
    Guid ProductVariantId,
    string VariantName,
    string VariantCode,
    string SellerSku,
    decimal PriceAmount,
    string CurrencyCode,
    string Status,
    string RowVersion,
    DateTimeOffset CreatedAtUtc);