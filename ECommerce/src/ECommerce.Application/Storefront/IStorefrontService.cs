using ECommerce.Application.Common;

namespace ECommerce.Application.Storefront;

public interface IStorefrontService
{
    Task<Result<PagedStorefrontListingsResponseDto>>
        SearchAsync(
            StorefrontQueryDto? query,
            CancellationToken cancellationToken = default);

    Task<Result<StorefrontListingResponseDto>>
        GetByIdAsync(
            Guid listingId,
            CancellationToken cancellationToken = default);
}