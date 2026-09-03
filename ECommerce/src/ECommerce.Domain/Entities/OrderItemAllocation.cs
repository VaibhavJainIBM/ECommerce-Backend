using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class OrderItemAllocation : Entity
{
    private OrderItemAllocation() { }

    internal OrderItemAllocation(Guid orderItemId, Guid inventoryItemId, int quantity)
    {
        if (orderItemId == Guid.Empty || inventoryItemId == Guid.Empty)
            throw new ArgumentException("Order item and inventory IDs are required.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        OrderItemId = orderItemId;
        InventoryItemId = inventoryItemId;
        Quantity = quantity;
    }

    public Guid OrderItemId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public int Quantity { get; private set; }
}
