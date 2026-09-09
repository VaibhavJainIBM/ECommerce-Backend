using ECommerce.Application.Common;

namespace ECommerce.Application.Storefront;

public static class StorefrontErrors
{
    public const string ListingNotFoundCode =
        "storefront.listing_not_found";

    public static readonly Error PageInvalid = new(
        "storefront.page_invalid",
        "Page must be greater than zero.");

    public static readonly Error PageSizeInvalid = new(
        "storefront.page_size_invalid",
        "Page size must be between 1 and 100.");

    public static readonly Error SearchTooLong = new(
        "storefront.search_too_long",
        "Search cannot exceed 100 characters.");

    public static readonly Error BrandTooLong = new(
        "storefront.brand_too_long",
        "Brand cannot exceed 150 characters.");

    public static readonly Error PriceInvalid = new(
        "storefront.price_invalid",
        "Price filters cannot be negative.");

    public static readonly Error PriceRangeInvalid = new(
        "storefront.price_range_invalid",
        "Minimum price cannot exceed maximum price.");

    public static readonly Error SortInvalid = new(
        "storefront.sort_invalid",
        "Sort must be name_asc, name_desc, price_asc, or price_desc.");

    public static readonly Error PaginationTooDeep = new(
        "storefront.pagination_too_deep",
        "The requested page is too large.");

    public static Error ListingNotFound(Guid listingId)
    {
        return new Error(
            ListingNotFoundCode,
            $"Active listing '{listingId}' was not found.");
    }
}
