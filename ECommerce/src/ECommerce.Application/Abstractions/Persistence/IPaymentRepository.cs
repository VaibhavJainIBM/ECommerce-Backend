using ECommerce.Application.Common;
using ECommerce.Application.Payments;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IPaymentRepository
{
    Task<Result<CreatePaymentResponseDto>> CreateDemoPaymentAsync(Guid customerId, Guid orderId, Guid requestKey, CancellationToken cancellationToken = default);
    Task<Result<PaymentResponseDto>> GetDemoPaymentAsync(Guid customerId, Guid paymentId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<PaymentResponseDto>>> GetDemoPaymentsAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default);
    Task<Result<PaymentResponseDto>> CompleteDemoPaymentAsync(Guid customerId, Guid paymentId, bool succeeded, CancellationToken cancellationToken = default);
}
