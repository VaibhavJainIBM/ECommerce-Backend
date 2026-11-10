namespace ECommerce.Payment.Application.Abstractions;

public sealed record OrderPaymentContext(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string CurrencyCode,
    string Status,
    DateTimeOffset ExpiresAtUtc);

public sealed record ConfirmOrderPaymentRequest(
    Guid PaymentId,
    decimal Amount,
    string CurrencyCode);

public interface IOrderPaymentClient
{
    Task<OrderPaymentContext?> GetPaymentContextAsync(
        Guid orderId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<bool> ConfirmPaymentAsync(
        Guid orderId,
        Guid customerId,
        ConfirmOrderPaymentRequest request,
        CancellationToken cancellationToken = default);
}