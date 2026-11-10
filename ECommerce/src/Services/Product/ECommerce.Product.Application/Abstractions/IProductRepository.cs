using ProductEntity = ECommerce.Product.Domain.Entities.Product;

namespace ECommerce.Product.Application.Abstractions;

public interface IProductRepository
{
    Task<IReadOnlyCollection<ProductEntity>>
        GetAllAsync(
            CancellationToken cancellationToken = default);

    Task<ProductEntity?>
        GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

    Task AddAsync(
        ProductEntity product,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}