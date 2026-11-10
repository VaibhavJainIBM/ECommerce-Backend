using ECommerce.Order.Domain.Entities;

namespace ECommerce.Order.Application.Abstractions;

public interface IOrderRepository
{
    Task<CustomerOrder?> GetByIdAsync(
        Guid orderId,
        Guid customerId,
        bool trackChanges,
        CancellationToken cancellationToken = default);

    Task<CustomerOrder?> GetByIdempotencyKeyAsync(
        Guid customerId,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerOrder>> GetForCustomerAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CustomerOrder order,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
