using ECommerce.Application.Common;

namespace ECommerce.Application.Inventory;

public static class InventoryErrors
{
    public const string SellerNotFoundCode =
        "inventory.seller_not_found";

    public const string WarehouseNotFoundCode =
        "inventory.warehouse_not_found";

    public const string ListingNotFoundCode =
        "inventory.listing_not_found";

    public const string InventoryNotFoundCode =
        "inventory.item_not_found";

    public const string SellerUnavailableCode =
        "inventory.seller_unavailable";

    public const string WarehouseUnavailableCode =
        "inventory.warehouse_unavailable";

    public const string ListingUnavailableCode =
        "inventory.listing_unavailable";

    public const string DuplicateInventoryCode =
        "inventory.duplicate_warehouse_listing";

    public static readonly Error RequestRequired = new(
        "inventory.request_required",
        "Inventory details are required.");

    public static readonly Error SellerIdRequired = new(
        "inventory.seller_id_required",
        "Seller ID is required.");

    public static readonly Error WarehouseIdRequired = new(
        "inventory.warehouse_id_required",
        "Warehouse ID is required.");

    public static readonly Error ListingIdRequired = new(
        "inventory.listing_id_required",
        "Seller listing ID is required.");

    public static readonly Error InventoryItemIdRequired = new(
        "inventory.item_id_required",
        "Inventory item ID is required.");

    public static readonly Error InitialQuantityInvalid = new(
        "inventory.initial_quantity_invalid",
        "Initial quantity cannot be negative.");

    public static readonly Error SellerNotFound = new(
        SellerNotFoundCode,
        "The seller was not found.");

    public static readonly Error WarehouseNotFound = new(
        WarehouseNotFoundCode,
        "The warehouse was not found.");

    public static readonly Error ListingNotFound = new(
        ListingNotFoundCode,
        "The seller listing was not found.");

    public static readonly Error DuplicateInventory = new(
        DuplicateInventoryCode,
        "Inventory already exists for this listing " +
        "in this warehouse.");

    public static Error InventoryItemNotFound(
        Guid inventoryItemId)
    {
        return new Error(
            InventoryNotFoundCode,
            $"Inventory item '{inventoryItemId}' was not found.");
    }

    public static Error SellerUnavailable(string status)
    {
        return new Error(
            SellerUnavailableCode,
            $"A seller with status '{status}' cannot " +
            "create inventory.");
    }

    public static Error WarehouseUnavailable(string status)
    {
        return new Error(
            WarehouseUnavailableCode,
            $"A warehouse with status '{status}' cannot " +
            "receive inventory.");
    }

    public static Error ListingUnavailable(string status)
    {
        return new Error(
            ListingUnavailableCode,
            $"A listing with status '{status}' cannot " +
            "receive inventory.");
    }
}