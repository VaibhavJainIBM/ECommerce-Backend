using ECommerce.Payment.Domain.Enums;

namespace ECommerce.Payment.Domain.Entities;

public sealed class PaymentAttempt
{
    private PaymentAttempt()
    {
    }

    public PaymentAttempt(
        Guid orderId,
        Guid customerId,
        Guid requestKey,
        decimal amount,
        string currencyCode)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException(
                "Order ID is required.",
                nameof(orderId));

        if (customerId == Guid.Empty)
            throw new ArgumentException(
                "Customer ID is required.",
                nameof(customerId));

        if (requestKey == Guid.Empty)
            throw new ArgumentException(
                "Request key is required.",
                nameof(requestKey));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount));

        if (string.IsNullOrWhiteSpace(currencyCode) ||
            currencyCode.Trim().Length != 3)
        {
            throw new ArgumentException(
                "Currency code must contain 3 characters.",
                nameof(currencyCode));
        }

        Id = Guid.NewGuid();
        OrderId = orderId;
        CustomerId = customerId;
        RequestKey = requestKey;
        Amount = decimal.Round(amount, 2);
        CurrencyCode =
            currencyCode.Trim().ToUpperInvariant();

        Status = PaymentStatus.Created;

        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid RequestKey { get; private set; }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; } =
        string.Empty;

    public PaymentStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } =
        Array.Empty<byte>();

    public void Complete(
        bool succeeded,
        DateTimeOffset now)
    {
        var target =
            succeeded
                ? PaymentStatus.Succeeded
                : PaymentStatus.Failed;

        if (Status == target)
            return;

        if (Status != PaymentStatus.Created)
        {
            throw new InvalidOperationException(
                "A completed payment cannot change outcome.");
        }

        Status = target;
        CompletedAtUtc = now;
    }

    public void Cancel(
        DateTimeOffset now)
    {
        if (Status != PaymentStatus.Created)
            return;

        Status = PaymentStatus.Cancelled;
        CompletedAtUtc = now;
    }
}