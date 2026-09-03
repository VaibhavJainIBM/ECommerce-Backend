using ECommerce.Domain.Common;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public sealed class OrderItem : Entity
{
    private OrderItem() { }

    public OrderItem(
        Guid sellerId,
        Guid listingId,
        Guid productVariantId,
        string sellerDisplayName,
        string productTitle,
        string variantName,
        string sellerSku,
        decimal unitPriceAmount,
        string currencyCode,
        int quantity)
    {
        if (sellerId == Guid.Empty || listingId == Guid.Empty || productVariantId == Guid.Empty)
            throw new ArgumentException("Seller, listing and variant IDs are required.");
        if (quantity is < 1 or > Cart.MaximumQuantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and 99.");
        if (unitPriceAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitPriceAmount), "Unit price must be greater than zero.");

        var price = new Money(unitPriceAmount, currencyCode);
        var lineTotal = new Money(price.Amount * quantity, price.CurrencyCode);
        SellerId = sellerId;
        SellerListingId = listingId;
        ProductVariantId = productVariantId;
        SellerDisplayName = Snapshot(sellerDisplayName, 150, nameof(sellerDisplayName));
        ProductTitle = Snapshot(productTitle, 250, nameof(productTitle));
        VariantName = Snapshot(variantName, 150, nameof(variantName));
        SellerSku = Snapshot(sellerSku, 64, nameof(sellerSku));
        UnitPriceAmount = price.Amount;
        CurrencyCode = price.CurrencyCode;
        Quantity = quantity;
        LineTotal = lineTotal.Amount;
    }

    public Guid OrderId { get; private set; }
    public Guid SellerId { get; private set; }
    public Guid SellerListingId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string SellerDisplayName { get; private set; } = string.Empty;
    public string ProductTitle { get; private set; } = string.Empty;
    public string VariantName { get; private set; } = string.Empty;
    public string SellerSku { get; private set; } = string.Empty;
    public decimal UnitPriceAmount { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }
    public DateTimeOffset? ShippedAtUtc { get; private set; }
    public ICollection<OrderItemAllocation> Allocations { get; private set; }
        = new List<OrderItemAllocation>();

    internal void AttachToOrder(Guid orderId)
    {
        if (OrderId != Guid.Empty && OrderId != orderId)
            throw new InvalidOperationException("An order item cannot belong to another order.");
        OrderId = orderId;
    }

    public void Allocate(InventoryItem item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Allocated quantity must be positive.");
        if (item.SellerId != SellerId || item.SellerListingId != SellerListingId)
            throw new InvalidOperationException("Inventory must belong to the ordered seller and listing.");
        if (Allocations.Sum(x => x.Quantity) + (long)quantity > Quantity)
            throw new InvalidOperationException("Cannot allocate more than the order item quantity.");
        if (Allocations.Any(x => x.InventoryItemId == item.Id))
            throw new InvalidOperationException("This inventory row is already allocated to the order item.");
        // Reserving inventory is coordinated transactionally by the checkout repository.
        Allocations.Add(new OrderItemAllocation(Id, item.Id, quantity));
    }

    public bool MarkShipped(DateTimeOffset now)
    {
        if (ShippedAtUtc.HasValue) return false;
        if (now == default)
            throw new ArgumentException("Shipment time is required.", nameof(now));
        if (Allocations.Sum(x => (long)x.Quantity) != Quantity)
            throw new InvalidOperationException("All ordered units must be allocated before shipment.");
        ShippedAtUtc = now.ToUniversalTime();
        return true;
    }

    private static string Snapshot(string value, int maximumLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new ArgumentException($"Value cannot exceed {maximumLength} characters.", parameterName);
        return normalized;
    }
}
