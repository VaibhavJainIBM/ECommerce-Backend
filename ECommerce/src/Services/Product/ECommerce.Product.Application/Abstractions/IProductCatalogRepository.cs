using ECommerce.Product.Domain.Entities;
using ProductEntity = ECommerce.Product.Domain.Entities.Product;

namespace ECommerce.Product.Application.Abstractions;

public interface IProductCatalogRepository
{
    Task<IReadOnlyCollection<string>>
        FindExistingGtinsAsync(
            IReadOnlyCollection<string> normalizedGtins,
            CancellationToken cancellationToken = default);

    Task<bool> TryCreateAsync(
        ProductEntity product,
        IReadOnlyCollection<ProductVariant> variants,
        CancellationToken cancellationToken = default);

    Task<ProductEntity?> GetWithVariantsAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}