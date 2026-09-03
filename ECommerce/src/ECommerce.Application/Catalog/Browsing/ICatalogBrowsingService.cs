using ECommerce.Application.Common;

namespace ECommerce.Application.Catalog.Browsing;

public interface ICatalogBrowsingService
{
    Task<Result<PagedCatalogProductsResponseDto>> SearchAsync(
        CatalogQueryDto? query,
        CancellationToken cancellationToken = default);

    Task<Result<CatalogProductResponseDto>> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}
