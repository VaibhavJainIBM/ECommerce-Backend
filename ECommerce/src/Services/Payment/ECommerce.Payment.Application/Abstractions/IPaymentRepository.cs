using ECommerce.Payment.Domain.Entities;

namespace ECommerce.Payment.Application.Abstractions;

public interface IPaymentRepository
{
    Task<PaymentAttempt?> GetByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<PaymentAttempt?> GetByOrderAndRequestKeyAsync(
        Guid orderId,
        Guid requestKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentAttempt>> GetByOrderAsync(
        Guid orderId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOpenPaymentAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PaymentAttempt payment,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}