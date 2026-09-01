namespace ECommerce.Application.Listings.Dtos;

public sealed class CreateSellerListingRequestDto
{
    public Guid ProductVariantId { get; init; }

    public string? SellerSku { get; init; }

    public decimal PriceAmount { get; init; }

    public string? CurrencyCode { get; init; }
}