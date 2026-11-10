using ECommerce.Payment.Application.Abstractions;
using ECommerce.Payment.Domain.Entities;
using ECommerce.Payment.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(
    PaymentDbContext dbContext)
    : IPaymentRepository
{
    public Task<PaymentAttempt?> GetByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PaymentAttempts
            .SingleOrDefaultAsync(
                x => x.Id == paymentId,
                cancellationToken);
    }

    public Task<PaymentAttempt?>
        GetByOrderAndRequestKeyAsync(
            Guid orderId,
            Guid requestKey,
            CancellationToken cancellationToken = default)
    {
        return dbContext.PaymentAttempts
            .SingleOrDefaultAsync(
                x =>
                    x.OrderId == orderId &&
                    x.RequestKey == requestKey,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentAttempt>>
        GetByOrderAsync(
            Guid orderId,
            Guid customerId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.PaymentAttempts
            .AsNoTracking()
            .Where(
                x =>
                    x.OrderId == orderId &&
                    x.CustomerId == customerId)
            .OrderByDescending(
                x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> HasOpenPaymentAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PaymentAttempts.AnyAsync(
            x =>
                x.OrderId == orderId &&
                x.Status == PaymentStatus.Created,
            cancellationToken);
    }

    public Task AddAsync(
        PaymentAttempt payment,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PaymentAttempts
            .AddAsync(
                payment,
                cancellationToken)
            .AsTask();
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}