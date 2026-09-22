using ECommerce.Payment.Application.Common;
using ECommerce.Payment.Application.Contracts;

namespace ECommerce.Payment.Application.Services;

public interface IPaymentService
{
    Task<Result<CreatePaymentResponse>> CreateAsync(
        Guid orderId,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Result<PaymentResponse>> GetAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PaymentResponse>>> GetForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<Result<PaymentResponse>> CompleteAsync(
        Guid paymentId,
        CompletePaymentRequest? request,
        CancellationToken cancellationToken = default);

}