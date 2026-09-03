using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public sealed class Order : AuditableEntity
{
    private Order() { }

    public Order(
        Guid customerId,
        Guid checkoutKey,
        string requestHash,
        string recipientName,
        string phone,
        Address shippingAddress,
        DateTimeOffset expiresAtUtc)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required.", nameof(customerId));
        if (checkoutKey == Guid.Empty)
            throw new ArgumentException("Checkout key is required.", nameof(checkoutKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentNullException.ThrowIfNull(shippingAddress);
        if (requestHash.Length != 64 || requestHash.Any(x => !Uri.IsHexDigit(x)))
            throw new ArgumentException("Request hash must be a SHA-256 hexadecimal value.", nameof(requestHash));
        if (recipientName.Trim().Length > 150)
            throw new ArgumentException("Recipient name cannot exceed 150 characters.", nameof(recipientName));
        if (phone.Trim().Length > 32)
            throw new ArgumentException("Phone cannot exceed 32 characters.", nameof(phone));

        CustomerId = customerId;
        CheckoutKey = checkoutKey;
        RequestHash = requestHash.ToUpperInvariant();
        OrderNumber = $"ORD-{Id:N}";
        RecipientName = recipientName.Trim();
        Phone = phone.Trim();
        ShippingAddress = shippingAddress;
        ExpiresAtUtc = expiresAtUtc;
        Status = OrderStatus.PendingPayment;
    }

    public Guid CustomerId { get; private set; }
    public Guid CheckoutKey { get; private set; }
    public string RequestHash { get; private set; } = string.Empty;
    public string OrderNumber { get; private set; } = string.Empty;
    public string RecipientName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public Address ShippingAddress { get; private set; } = null!;
    public string CurrencyCode { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }
    public string? PaymentMode { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();

    public void AddItem(OrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Status != OrderStatus.PendingPayment || RowVersion.Length != 0)
            throw new InvalidOperationException("Items can only be added while constructing a new pending order.");
        if (Items.Count >= Cart.MaximumLines)
            throw new InvalidOperationException("An order cannot contain more than 50 distinct listings.");
        if (Items.Any(x => x.SellerListingId == item.SellerListingId))
            throw new InvalidOperationException("The listing already exists in this order.");
        if (Items.Count > 0 && item.CurrencyCode != CurrencyCode)
            throw new InvalidOperationException("All order items must use the same currency.");

        // Money validates SQL decimal(18,2) precision and maximum before mutation.
        var total = new Money(TotalAmount + item.LineTotal, item.CurrencyCode);
        item.AttachToOrder(Id);
        Items.Add(item);
        CurrencyCode = total.CurrencyCode;
        TotalAmount = total.Amount;
        MarkUpdated();
    }

    public void MarkPaid(DateTimeOffset now)
    {
        if (Status != OrderStatus.PendingPayment || ExpiresAtUtc <= now)
            throw new InvalidOperationException("Only an unexpired pending order can be paid.");
        if (Items.Count == 0 || TotalAmount <= 0)
            throw new InvalidOperationException("An empty order cannot be paid.");
        Status = OrderStatus.Paid;
        PaidAtUtc = now;
        PaymentMode = "Demo";
        MarkUpdated();
    }

    public void RefreshShipmentStatus()
    {
        if (Status is not (OrderStatus.Paid or OrderStatus.PartiallyShipped))
            throw new InvalidOperationException("Only a paid order can be shipped.");
        if (!Items.Any(x => x.ShippedAtUtc.HasValue))
            throw new InvalidOperationException("No order item has been shipped.");
        Status = Items.All(x => x.ShippedAtUtc.HasValue)
            ? OrderStatus.Shipped : OrderStatus.PartiallyShipped;
        MarkUpdated();
    }

    public bool Cancel()
    {
        if (Status is OrderStatus.Cancelled or OrderStatus.Expired)
            return false;
        if (Status != OrderStatus.PendingPayment)
            throw new InvalidOperationException("Only a pending order can be cancelled.");
        Status = OrderStatus.Cancelled;
        MarkUpdated();
        return true;
    }

    public bool Expire(DateTimeOffset now)
    {
        if (Status != OrderStatus.PendingPayment || ExpiresAtUtc > now)
            return false;
        Status = OrderStatus.Expired;
        MarkUpdated();
        return true;
    }
}
