using ECommerce.Application.Common;
using ECommerce.Application.Listings.Dtos;

namespace ECommerce.Application.Listings;

public interface ISellerListingService
{
    Task<Result<SellerListingResponseDto>> CreateAsync(
        Guid sellerId,
        CreateSellerListingRequestDto? request,
        CancellationToken cancellationToken = default);
}