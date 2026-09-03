namespace ECommerce.Application.Payments;

public sealed record DemoPaymentMode(bool Enabled);

public sealed class CompleteDemoPaymentRequestDto
{
    public string? Outcome { get; init; }
}

public sealed record PaymentResponseDto(
    Guid PaymentId, Guid OrderId, string Mode, string Status,
    decimal Amount, string CurrencyCode, string OrderStatus,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? CompletedAtUtc, string RowVersion);

public sealed record CreatePaymentResponseDto(PaymentResponseDto Payment, bool Replayed);
