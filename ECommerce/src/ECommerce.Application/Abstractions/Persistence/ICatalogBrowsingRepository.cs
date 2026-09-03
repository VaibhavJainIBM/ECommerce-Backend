using ECommerce.Application.Catalog.Browsing;

namespace ECommerce.Application.Abstractions.Persistence;

public interface ICatalogBrowsingRepository
{
    Task<CatalogProductPage> SearchAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<CatalogProductResponseDto?> FindByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}
