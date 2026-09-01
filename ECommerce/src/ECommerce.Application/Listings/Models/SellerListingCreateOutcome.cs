namespace ECommerce.Application.Listings.Models;

public enum SellerListingCreateOutcome
{
    Created = 1,
    DuplicateSellerSku = 2,
    DuplicateSellerVariant = 3
}