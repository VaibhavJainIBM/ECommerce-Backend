using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class CartItem : Entity
{
    private CartItem() { }

    internal CartItem(Guid cartId, Guid sellerListingId, int quantity)
    {
        CartId = cartId;
        SellerListingId = sellerListingId;
        SetQuantity(quantity);
    }

    public Guid CartId { get; private set; }
    public Guid SellerListingId { get; private set; }
    public int Quantity { get; private set; }

    internal void SetQuantity(int quantity)
    {
        if (quantity is < 1 or > Cart.MaximumQuantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and 99.");
        Quantity = quantity;
    }
}
