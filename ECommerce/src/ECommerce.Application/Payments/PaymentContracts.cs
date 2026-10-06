namespace ECommerce.Application.Payments;

public sealed record DemoPaymentMode(bool Enabled)
{
    public static DemoPaymentMode Resolve(
        string? environmentName,
        bool requested)
    {
        if (!requested)
        {
            return new DemoPaymentMode(false);
        }

        var allowed =
            string.Equals(
                environmentName,
                "Development",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                environmentName,
                "Staging",
                StringComparison.OrdinalIgnoreCase);

        if (!allowed)
        {
            throw new InvalidOperationException(
                "Demo payments cannot be enabled in environment " +
                $"'{environmentName ?? "<null>"}'.");
        }

        return new DemoPaymentMode(true);
    }
}

public sealed class CompleteDemoPaymentRequestDto
{
    public string? Outcome { get; init; }
}

public sealed record PaymentResponseDto(
    Guid PaymentId, Guid OrderId, string Mode, string Status,
    decimal Amount, string CurrencyCode, string OrderStatus,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? CompletedAtUtc, string RowVersion);

public sealed record CreatePaymentResponseDto(PaymentResponseDto Payment, bool Replayed);
