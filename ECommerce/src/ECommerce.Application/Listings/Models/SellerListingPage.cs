namespace ECommerce.Application.Listings.Models;

public sealed record SellerListingPage(
    IReadOnlyCollection<SellerListingReadModel> Items,
    int TotalCount);