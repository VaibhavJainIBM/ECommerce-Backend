using ECommerce.Product.Application.Common;

namespace ECommerce.Product.Application.Catalog.Browsing;

public interface ICatalogBrowsingService
{
    Task<Result<PagedCatalogProductsResponseDto>> SearchAsync(
        CatalogQueryDto? query,
        CancellationToken cancellationToken = default);

    Task<Result<CatalogProductResponseDto>> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}
