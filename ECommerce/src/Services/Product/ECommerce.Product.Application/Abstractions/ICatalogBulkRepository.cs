using ECommerce.Product.Domain.Entities;
using ProductEntity = ECommerce.Product.Domain.Entities.Product;

namespace ECommerce.Product.Application.Abstractions;

public interface ICatalogBulkRepository
{
    Task<IReadOnlyCollection<string>>
        FindExistingGtinsAsync(
            IReadOnlyCollection<string> normalizedGtins,
            CancellationToken cancellationToken = default);

    Task<bool> TryCreateBatchAsync(
        IReadOnlyCollection<ProductEntity> products,
        IReadOnlyCollection<ProductVariant> variants,
        CancellationToken cancellationToken = default);
}
