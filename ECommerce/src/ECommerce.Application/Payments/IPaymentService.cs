using ECommerce.Application.Common;

namespace ECommerce.Application.Payments;

public interface IPaymentService
{
    Task<Result<CreatePaymentResponseDto>> CreateAsync(Guid orderId, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<Result<PaymentResponseDto>> GetAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<PaymentResponseDto>>> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Result<PaymentResponseDto>> CompleteAsync(Guid paymentId, CompleteDemoPaymentRequestDto? request, CancellationToken cancellationToken = default);
}
