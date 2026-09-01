using ECommerce.Domain.Enums;

namespace ECommerce.Application.Listings.Models;

public sealed record SellerListingVariantSnapshot(
    Guid ProductId,
    Guid ProductVariantId,
    string ProductTitle,
    string BrandName,
    string VariantName,
    string VariantCode,
    ProductStatus ProductStatus,
    ProductVariantStatus VariantStatus);