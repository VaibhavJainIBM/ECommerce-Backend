using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class SellerLifecycleRepository(
    ECommerceDbContext dbContext)
    : ISellerLifecycleRepository
{
    public async Task<Seller?> GetTrackedAsync(
        Guid sellerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Sellers
            .SingleOrDefaultAsync(
                seller => seller.Id == sellerId,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}