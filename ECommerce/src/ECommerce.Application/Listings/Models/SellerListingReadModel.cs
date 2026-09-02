using ECommerce.Domain.Enums;

namespace ECommerce.Application.Listings.Models;

public sealed record SellerListingReadModel(
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
    SellerListingStatus Status,
    byte[] RowVersion,
    DateTimeOffset CreatedAtUtc);