using ECommerce.Order.Application.Abstractions;
using ECommerce.Order.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Order.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(
    OrderDbContext dbContext)
    : IOrderRepository
{
    public async Task<CustomerOrder?> GetByIdAsync(
        Guid orderId,
        Guid customerId,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders
            .Include(x => x.Items)
            .Where(x => x.Id == orderId &&
                        x.CustomerId == customerId);

        if (!trackChanges)
            query = query.AsNoTracking();

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<CustomerOrder?> GetByIdempotencyKeyAsync(
        Guid customerId,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(
                x => x.CustomerId == customerId &&
                     x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerOrder>> GetForCustomerAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public Task AddAsync(
        CustomerOrder order,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Orders
            .AddAsync(order, cancellationToken)
            .AsTask();
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
