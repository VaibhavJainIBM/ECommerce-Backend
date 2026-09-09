using ECommerce.Application.Storefront;

namespace ECommerce.Application.Abstractions.Persistence;

public interface IStorefrontRepository
{
    Task<StorefrontListingPage> SearchAsync(
        StorefrontSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<StorefrontListingReadModel?> FindByIdAsync(
        Guid listingId,
        CancellationToken cancellationToken = default);
}
