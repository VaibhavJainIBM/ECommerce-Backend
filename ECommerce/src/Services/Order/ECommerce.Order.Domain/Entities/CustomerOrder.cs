using ECommerce.Order.Domain.Enums;

namespace ECommerce.Order.Domain.Entities;

public sealed record OrderLineSnapshot(
    Guid SellerId,
    Guid SellerListingId,
    Guid ProductVariantId,
    string SellerSku,
    string ProductTitle,
    string VariantName,
    int Quantity,
    decimal UnitPrice);

public sealed record ShippingAddressSnapshot(
    string FullName,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string CountryCode);

public sealed class CustomerOrder
{
    private readonly List<OrderItem> _items = [];

    private CustomerOrder()
    {
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid IdempotencyKey { get; private set; }

    public OrderStatus Status { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public string ShippingFullName { get; private set; } = string.Empty;

    public string ShippingLine1 { get; private set; } = string.Empty;

    public string? ShippingLine2 { get; private set; }

    public string ShippingCity { get; private set; } = string.Empty;

    public string ShippingState { get; private set; } = string.Empty;

    public string ShippingPostalCode { get; private set; } = string.Empty;

    public string ShippingCountryCode { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? PaidAtUtc { get; private set; }

    public Guid? PaymentId { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<OrderItem> Items => _items;

    public static CustomerOrder Create(
        Guid customerId,
        Guid idempotencyKey,
        string currencyCode,
        ShippingAddressSnapshot shippingAddress,
        IReadOnlyCollection<OrderLineSnapshot> lines,
        DateTimeOffset nowUtc,
        TimeSpan paymentWindow)
    {
        if (customerId == Guid.Empty || idempotencyKey == Guid.Empty)
            throw new ArgumentException("Customer and idempotency identifiers are required.");

        if (lines.Count == 0)
            throw new ArgumentException("At least one order item is required.", nameof(lines));

        if (paymentWindow <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(paymentWindow));

        var normalizedCurrency = currencyCode?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedCurrency) ||
            normalizedCurrency.Length != 3)
        {
            throw new ArgumentException("Currency code must contain three letters.");
        }

        ValidateAddress(shippingAddress);

        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            IdempotencyKey = idempotencyKey,
            Status = OrderStatus.PendingPayment,
            CurrencyCode = normalizedCurrency,
            ShippingFullName = shippingAddress.FullName.Trim(),
            ShippingLine1 = shippingAddress.Line1.Trim(),
            ShippingLine2 = string.IsNullOrWhiteSpace(shippingAddress.Line2)
                ? null
                : shippingAddress.Line2.Trim(),
            ShippingCity = shippingAddress.City.Trim(),
            ShippingState = shippingAddress.State.Trim(),
            ShippingPostalCode = shippingAddress.PostalCode.Trim(),
            ShippingCountryCode = shippingAddress.CountryCode.Trim().ToUpperInvariant(),
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.Add(paymentWindow)
        };

        foreach (var line in lines)
        {
            order._items.Add(
                new OrderItem(
                    order.Id,
                    line.SellerId,
                    line.SellerListingId,
                    line.ProductVariantId,
                    line.SellerSku,
                    line.ProductTitle,
                    line.VariantName,
                    line.Quantity,
                    line.UnitPrice));
        }

        order.TotalAmount = decimal.Round(
            order._items.Sum(x => x.LineTotal),
            2);

        return order;
    }

    public void Cancel(string? reason, DateTimeOffset nowUtc)
    {
        if (Status == OrderStatus.Cancelled)
            return;

        if (Status != OrderStatus.PendingPayment)
            throw new InvalidOperationException("Only a pending-payment order can be cancelled.");

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = nowUtc;
        CancellationReason = string.IsNullOrWhiteSpace(reason)
            ? "Cancelled by customer."
            : reason.Trim();
    }

    public void ConfirmPayment(
        Guid paymentId,
        decimal amount,
        string currencyCode,
        DateTimeOffset nowUtc)
    {
        if (Status == OrderStatus.Paid && PaymentId == paymentId)
            return;

        if (Status != OrderStatus.PendingPayment)
            throw new InvalidOperationException("The order is not awaiting payment.");

        if (ExpiresAtUtc <= nowUtc)
        {
            Status = OrderStatus.Expired;
            throw new InvalidOperationException("The payment window has expired.");
        }

        if (paymentId == Guid.Empty ||
            amount != TotalAmount ||
            !string.Equals(currencyCode?.Trim(), CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Payment details do not match this order.");
        }

        PaymentId = paymentId;
        PaidAtUtc = nowUtc;
        Status = OrderStatus.Paid;
    }

    private static void ValidateAddress(ShippingAddressSnapshot address)
    {
        if (string.IsNullOrWhiteSpace(address.FullName) ||
            string.IsNullOrWhiteSpace(address.Line1) ||
            string.IsNullOrWhiteSpace(address.City) ||
            string.IsNullOrWhiteSpace(address.State) ||
            string.IsNullOrWhiteSpace(address.PostalCode) ||
            string.IsNullOrWhiteSpace(address.CountryCode))
        {
            throw new ArgumentException("A complete shipping address is required.");
        }

        if (address.CountryCode.Trim().Length != 2)
            throw new ArgumentException("Country code must contain two letters.");
    }
}
