using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions.Persistence;

public interface ICatalogBulkRepository
{
    Task<IReadOnlyCollection<string>>
        FindExistingGtinsAsync(
            IReadOnlyCollection<string> normalizedGtins,
            CancellationToken cancellationToken = default);

    Task<bool> TryCreateBatchAsync(
        IReadOnlyCollection<Product> products,
        IReadOnlyCollection<ProductVariant> variants,
        CancellationToken cancellationToken = default);
}
