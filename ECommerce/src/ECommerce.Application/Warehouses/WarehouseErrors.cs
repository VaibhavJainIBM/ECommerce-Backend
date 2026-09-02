using ECommerce.Application.Common;

namespace ECommerce.Application.Warehouses;

public static class WarehouseErrors
{
    public const string SellerNotFoundCode =
        "warehouse.seller_not_found";

    public const string WarehouseNotFoundCode =
        "warehouse.not_found";

    public const string SellerUnavailableCode =
        "warehouse.seller_unavailable";

    public const string DuplicateCodeCode =
        "warehouse.duplicate_code";

    public static readonly Error SellerIdRequired = new(
        "warehouse.seller_id_required",
        "Seller ID is required.");

    public static readonly Error WarehouseIdRequired = new(
        "warehouse.warehouse_id_required",
        "Warehouse ID is required.");

    public static readonly Error RequestRequired = new(
        "warehouse.request_required",
        "Warehouse details are required.");

    public static readonly Error NameRequired = new(
        "warehouse.name_required",
        "Warehouse name is required.");

    public static readonly Error NameTooLong = new(
        "warehouse.name_too_long",
        "Warehouse name cannot exceed 150 characters.");

    public static readonly Error CodeRequired = new(
        "warehouse.code_required",
        "Warehouse code is required.");

    public static readonly Error CodeTooLong = new(
        "warehouse.code_too_long",
        "Warehouse code cannot exceed 50 characters.");

    public static readonly Error CodeInvalid = new(
        "warehouse.code_invalid",
        "Warehouse code may contain only letters, numbers, " +
        "hyphens, and underscores.");

    public static readonly Error AddressRequired = new(
        "warehouse.address_required",
        "Warehouse address is required.");

    public static readonly Error AddressLine1Required = new(
        "warehouse.address_line1_required",
        "Address line 1 is required.");

    public static readonly Error AddressLine1TooLong = new(
        "warehouse.address_line1_too_long",
        "Address line 1 cannot exceed 200 characters.");

    public static readonly Error AddressLine2TooLong = new(
        "warehouse.address_line2_too_long",
        "Address line 2 cannot exceed 200 characters.");

    public static readonly Error CityRequired = new(
        "warehouse.city_required",
        "City is required.");

    public static readonly Error CityTooLong = new(
        "warehouse.city_too_long",
        "City cannot exceed 100 characters.");

    public static readonly Error StateRequired = new(
        "warehouse.state_required",
        "State or province is required.");

    public static readonly Error StateTooLong = new(
        "warehouse.state_too_long",
        "State or province cannot exceed 100 characters.");

    public static readonly Error PostalCodeRequired = new(
        "warehouse.postal_code_required",
        "Postal code is required.");

    public static readonly Error PostalCodeTooLong = new(
        "warehouse.postal_code_too_long",
        "Postal code cannot exceed 20 characters.");

    public static readonly Error CountryCodeRequired = new(
        "warehouse.country_code_required",
        "Country code is required.");

    public static readonly Error CountryCodeInvalid = new(
        "warehouse.country_code_invalid",
        "Country code must contain exactly two letters.");

    public static readonly Error SellerNotFound = new(
        SellerNotFoundCode,
        "The seller was not found.");

    public static readonly Error DuplicateCode = new(
        DuplicateCodeCode,
        "This warehouse code is already used by the seller.");

    public static Error SellerUnavailable(string status)
    {
        return new Error(
            SellerUnavailableCode,
            $"A seller with status '{status}' cannot manage warehouses.");
    }

    public static Error WarehouseNotFound(Guid warehouseId)
    {
        return new Error(
            WarehouseNotFoundCode,
            $"Warehouse '{warehouseId}' was not found.");
    }
}