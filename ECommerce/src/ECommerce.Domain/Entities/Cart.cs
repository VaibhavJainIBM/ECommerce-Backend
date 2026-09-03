using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Cart : AuditableEntity
{
    public const int MaximumLines = 50;
    public const int MaximumQuantity = 99;

    private Cart() { }

    public Cart(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required.", nameof(customerId));
        CustomerId = customerId;
    }

    public Guid CustomerId { get; private set; }
    public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void SetItem(Guid listingId, int quantity)
    {
        if (listingId == Guid.Empty)
            throw new ArgumentException("Listing ID is required.", nameof(listingId));
        if (quantity is < 1 or > MaximumQuantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and 99.");

        var item = Items.SingleOrDefault(x => x.SellerListingId == listingId);
        if (item is null)
        {
            if (Items.Count >= MaximumLines)
                throw new InvalidOperationException("A cart cannot contain more than 50 distinct listings.");
            Items.Add(new CartItem(Id, listingId, quantity));
        }
        else
        {
            item.SetQuantity(quantity);
        }
        MarkUpdated();
    }

    public void RemoveItem(Guid listingId)
    {
        if (listingId == Guid.Empty)
            throw new ArgumentException("Listing ID is required.", nameof(listingId));
        var item = Items.SingleOrDefault(x => x.SellerListingId == listingId);
        if (item is not null)
            Items.Remove(item);
        MarkUpdated();
    }

    public void Clear()
    {
        Items.Clear();
        MarkUpdated();
    }
}
