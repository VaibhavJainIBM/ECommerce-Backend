using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IProductCatalogRepository
{
    Task<IReadOnlyCollection<string>>
        FindExistingGtinsAsync(
            IReadOnlyCollection<string> normalizedGtins,
            CancellationToken cancellationToken = default);

    Task<bool> TryCreateAsync(
        Product product,
        IReadOnlyCollection<ProductVariant> variants,
        CancellationToken cancellationToken = default);

    Task<Product?> GetWithVariantsAsync(
    Guid productId,
    CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}