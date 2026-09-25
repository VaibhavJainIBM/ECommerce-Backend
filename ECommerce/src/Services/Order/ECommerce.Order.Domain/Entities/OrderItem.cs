namespace ECommerce.Order.Domain.Entities;

public sealed class OrderItem
{
    private OrderItem()
    {
    }

    internal OrderItem(
        Guid orderId,
        Guid sellerId,
        Guid sellerListingId,
        Guid productVariantId,
        string sellerSku,
        string productTitle,
        string variantName,
        int quantity,
        decimal unitPrice)
    {
        if (orderId == Guid.Empty ||
            sellerId == Guid.Empty ||
            sellerListingId == Guid.Empty ||
            productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order and catalog identifiers are required.");
        }

        if (string.IsNullOrWhiteSpace(sellerSku) ||
            string.IsNullOrWhiteSpace(productTitle) ||
            string.IsNullOrWhiteSpace(variantName))
        {
            throw new ArgumentException(
                "SKU and product snapshot details are required.");
        }

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        Id = Guid.NewGuid();
        OrderId = orderId;
        SellerId = sellerId;
        SellerListingId = sellerListingId;
        ProductVariantId = productVariantId;
        SellerSku = sellerSku.Trim();
        ProductTitle = productTitle.Trim();
        VariantName = variantName.Trim();
        Quantity = quantity;
        UnitPrice = decimal.Round(unitPrice, 2);
        LineTotal = decimal.Round(UnitPrice * quantity, 2);
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid SellerId { get; private set; }

    public Guid SellerListingId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public string SellerSku { get; private set; } = string.Empty;

    public string ProductTitle { get; private set; } = string.Empty;

    public string VariantName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal { get; private set; }
}
