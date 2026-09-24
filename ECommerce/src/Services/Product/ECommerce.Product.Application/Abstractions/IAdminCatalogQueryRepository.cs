using ECommerce.Product.Application.Catalog;
using ECommerce.Product.Domain.Enums;

namespace ECommerce.Product.Application.Abstractions;

public interface IAdminCatalogQueryRepository
{
    Task<AdminCatalogPage> GetProductsAsync(
        string? search,
        ProductStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}