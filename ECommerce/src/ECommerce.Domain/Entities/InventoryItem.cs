using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class InventoryItem : AuditableEntity
{
    private InventoryItem()
    {
    }

    public InventoryItem(
        Warehouse warehouse,
        SellerListing sellerListing)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentNullException.ThrowIfNull(sellerListing);

        if (warehouse.SellerId != sellerListing.SellerId)
        {
            throw new InvalidOperationException(
                "A listing cannot be stocked in another " +
                "seller's warehouse.");
        }

        SellerId = warehouse.SellerId;

        WarehouseId = warehouse.Id;
        Warehouse = warehouse;

        SellerListingId = sellerListing.Id;
        SellerListing = sellerListing;

        OnHandQuantity = 0;
        ReservedQuantity = 0;
    }

    public Guid SellerId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; } = null!;

    public Guid SellerListingId { get; private set; }

    public SellerListing SellerListing { get; private set; } = null!;

    public int OnHandQuantity { get; private set; }

    public int ReservedQuantity { get; private set; }

    public int AvailableQuantity =>
        OnHandQuantity - ReservedQuantity;

    public byte[] RowVersion { get; private set; }
        = Array.Empty<byte>();

    public void Receive(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        OnHandQuantity = checked(
            OnHandQuantity + quantity);

        MarkUpdated();
    }

    public void Reserve(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > AvailableQuantity)
        {
            throw new InvalidOperationException(
                "Not enough available inventory.");
        }

        ReservedQuantity = checked(
            ReservedQuantity + quantity);

        MarkUpdated();
    }

    public void Release(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > ReservedQuantity)
        {
            throw new InvalidOperationException(
                "Cannot release more inventory than is reserved.");
        }

        ReservedQuantity -= quantity;

        MarkUpdated();
    }

    public void Ship(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > ReservedQuantity)
        {
            throw new InvalidOperationException(
                "Cannot ship more inventory than is reserved.");
        }

        ReservedQuantity -= quantity;
        OnHandQuantity -= quantity;

        MarkUpdated();
    }

    public void AdjustOnHand(int newOnHandQuantity)
    {
        if (newOnHandQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newOnHandQuantity),
                "On-hand quantity cannot be negative.");
        }

        if (newOnHandQuantity < ReservedQuantity)
        {
            throw new InvalidOperationException(
                "On-hand quantity cannot be lower than " +
                "the reserved quantity.");
        }

        OnHandQuantity = newOnHandQuantity;

        MarkUpdated();
    }

    private static void EnsurePositiveQuantity(
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }
    }
}