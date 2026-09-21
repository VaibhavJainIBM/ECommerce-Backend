using ECommerce.Application.Administration;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IAdminQueryRepository
{
    Task<AdminQueryPage<AdminSellerListItemDto>>
        GetSellersAsync(
            string? search,
            SellerStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default);

    Task<AdminQueryPage<AdminListingListItemDto>>
        GetListingsAsync(
            string? search,
            SellerListingStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default);

    Task<AdminQueryPage<AdminCatalogProductListItemDto>>
        GetCatalogProductsAsync(
            string? search,
            ProductStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
}
