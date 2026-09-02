namespace ECommerce.Application.Listings.Dtos;

public sealed class SellerListingQueryDto
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Status { get; init; }
}