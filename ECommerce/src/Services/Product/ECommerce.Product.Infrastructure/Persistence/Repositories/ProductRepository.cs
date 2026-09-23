using ECommerce.Product.Application.Abstractions;
using ECommerce.Product.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ProductEntity = ECommerce.Product.Domain.Entities.Product;

namespace ECommerce.Product.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(
    ProductDbContext dbContext)
    : IProductRepository
{
    public async Task<IReadOnlyCollection<ProductEntity>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Include(x => x.Variants)
            .OrderBy(x => x.Title)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ProductEntity?>
        GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .Include(x => x.Variants)
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        ProductEntity product,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Products.AddAsync(
            product,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}