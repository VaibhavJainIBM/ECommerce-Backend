using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

// A development-only simulation record. No card data or provider credentials are stored.
public sealed class DemoPayment : AuditableEntity
{
    private DemoPayment() { }

    public DemoPayment(Guid orderId, Guid requestKey, decimal amount, string currencyCode)
    {
        if (orderId == Guid.Empty || requestKey == Guid.Empty)
            throw new ArgumentException("Order ID and payment request key are required.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var money = new Money(amount, currencyCode);
        OrderId = orderId;
        RequestKey = requestKey;
        Amount = money.Amount;
        CurrencyCode = money.CurrencyCode;
    }

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public Guid RequestKey { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public DemoPaymentStatus Status { get; private set; } = DemoPaymentStatus.Created;
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void Complete(bool succeeded, DateTimeOffset now)
    {
        var target = succeeded ? DemoPaymentStatus.Succeeded : DemoPaymentStatus.Failed;
        if (Status == target) return;
        if (Status != DemoPaymentStatus.Created)
            throw new InvalidOperationException("A completed payment attempt cannot change its outcome.");
        Status = target;
        CompletedAtUtc = now;
        MarkUpdated();
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status != DemoPaymentStatus.Created) return;
        Status = DemoPaymentStatus.Cancelled;
        CompletedAtUtc = now;
        MarkUpdated();
    }
}
