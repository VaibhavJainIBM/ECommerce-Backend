using ECommerce.Product.Application.Common;

namespace ECommerce.Product.Application.Catalog;

public interface IAdminCatalogQueryService
{
    Task<Result<PagedAdminCatalogProductsResponseDto>>
        GetProductsAsync(
            AdminCatalogQueryDto? query,
            CancellationToken cancellationToken = default);
}