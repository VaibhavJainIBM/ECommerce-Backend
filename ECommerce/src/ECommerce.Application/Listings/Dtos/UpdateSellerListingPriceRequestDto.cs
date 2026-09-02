namespace ECommerce.Application.Listings.Dtos;

public sealed class UpdateSellerListingPriceRequestDto
{
    public decimal PriceAmount { get; init; }

    public string? CurrencyCode { get; init; }

    public string? RowVersion { get; init; }
}