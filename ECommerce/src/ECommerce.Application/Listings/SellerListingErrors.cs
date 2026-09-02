using ECommerce.Application.Common;

namespace ECommerce.Application.Listings;

public static class SellerListingErrors
{
    public const string SellerNotFoundCode =
        "listing.seller_not_found";
    public const string ListingStateConflictCode =
    "listing.state_conflict";

    public const string ConcurrencyConflictCode =
        "listing.concurrency_conflict";
    public const string ListingNotFoundCode =
    "listing.listing_not_found";

    public const string VariantNotFoundCode =
        "listing.variant_not_found";

    public const string SellerUnavailableCode =
        "listing.seller_unavailable";

    public const string CatalogUnavailableCode =
        "listing.catalog_unavailable";

    public const string DuplicateSellerSkuCode =
        "listing.duplicate_seller_sku";

    public const string DuplicateSellerVariantCode =
        "listing.duplicate_seller_variant";

    public static readonly Error RequestRequired = new(
        "listing.request_required",
        "Listing details are required.");

    public static readonly Error SellerIdRequired = new(
        "listing.seller_id_required",
        "Seller ID is required.");

    public static readonly Error VariantIdRequired = new(
        "listing.variant_id_required",
        "Product variant ID is required.");

    public static readonly Error PriceUpdateRequestRequired = new(
    "listing.price_update_request_required",
    "Price update details are required.");

    public static readonly Error ArchiveRequestRequired = new(
        "listing.archive_request_required",
        "Archive details are required.");

    public static readonly Error RowVersionRequired = new(
        "listing.row_version_required",
        "Row version is required.");

    public static readonly Error RowVersionInvalid = new(
        "listing.row_version_invalid",
        "Row version must be a valid Base64-encoded " +
        "SQL Server rowversion.");

    public static Error PriceChangeNotAllowed(string status)
    {
        return new Error(
            ListingStateConflictCode,
            $"A listing with status '{status}' " +
            "cannot change price.");
    }

    public static Error SellerCannotChangePrice(string status)
    {
        return new Error(
            SellerUnavailableCode,
            $"A seller with status '{status}' " +
            "cannot change listing prices.");
    }

    public static readonly Error ConcurrencyConflict = new(
        ConcurrencyConflictCode,
        "The listing was changed by another request. " +
        "Reload it and try again using the latest row version.");
    public static readonly Error SellerSkuRequired = new(
        "listing.seller_sku_required",
        "Seller SKU is required.");

    public static readonly Error SellerSkuTooLong = new(
        "listing.seller_sku_too_long",
        "Seller SKU cannot exceed 64 characters.");

    public static readonly Error SellerSkuInvalid = new(
        "listing.seller_sku_invalid",
        "Seller SKU may contain only letters, numbers, " +
        "hyphens, underscores, and periods.");

    public static readonly Error PriceMustBePositive = new(
        "listing.price_must_be_positive",
        "Listing price must be greater than zero.");

    public static readonly Error PriceTooLarge = new(
        "listing.price_too_large",
        "Listing price exceeds the supported maximum.");

    public static readonly Error PriceTooPrecise = new(
        "listing.price_too_precise",
        "Listing price cannot contain more than two decimal places.");

    public static readonly Error CurrencyRequired = new(
        "listing.currency_required",
        "Currency code is required.");

    public static readonly Error CurrencyInvalid = new(
        "listing.currency_invalid",
        "Currency code must contain exactly three letters.");

    public static readonly Error SellerNotFound = new(
        SellerNotFoundCode,
        "The seller was not found.");

    public static readonly Error ListingIdRequired = new(
    "listing.listing_id_required",
    "Listing ID is required.");

    public static readonly Error PageInvalid = new(
        "listing.page_invalid",
        "Page must be greater than zero.");

    public static readonly Error PageSizeInvalid = new(
        "listing.page_size_invalid",
        "Page size must be between 1 and 100.");

    public static readonly Error PaginationTooDeep = new(
        "listing.pagination_too_deep",
        "The requested page is too large.");

    public static Error InvalidStatus(string status)
    {
        return new Error(
            "listing.invalid_status",
            $"'{status}' is not a valid listing status.");
    }

    public static Error ListingNotFound(Guid listingId)
    {
        return new Error(
            ListingNotFoundCode,
            $"Listing '{listingId}' was not found.");
    }
    public static readonly Error VariantNotFound = new(
        VariantNotFoundCode,
        "The product variant was not found.");

    public static Error SellerUnavailable(string status)
    {
        return new Error(
            SellerUnavailableCode,
            $"A seller with status '{status}' cannot create listings.");
    }

    public static readonly Error ProductNotActive = new(
        CatalogUnavailableCode,
        "The shared product is not active.");

    public static readonly Error VariantNotActive = new(
        CatalogUnavailableCode,
        "The shared product variant is not active.");

    public static readonly Error DuplicateSellerSku = new(
        DuplicateSellerSkuCode,
        "This seller SKU is already in use.");

    public static readonly Error DuplicateSellerVariant = new(
        DuplicateSellerVariantCode,
        "This seller already has a listing for this product variant.");
}