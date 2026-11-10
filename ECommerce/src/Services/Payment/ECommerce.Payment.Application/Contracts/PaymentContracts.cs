namespace ECommerce.Payment.Application.Contracts;

public sealed record PaymentResponse(
    Guid PaymentId,
    Guid OrderId,
    string Status,
    decimal Amount,
    string CurrencyCode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record CreatePaymentResponse(
    PaymentResponse Payment,
    bool Replayed);

public sealed record CompletePaymentRequest(
    string Outcome);