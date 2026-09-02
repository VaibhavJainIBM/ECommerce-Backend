using ECommerce.Application.Common;
using ECommerce.Application.Listings.Dtos;

namespace ECommerce.Application.Listings;

public interface ISellerListingService
{
    Task<Result<SellerListingResponseDto>> CreateAsync(
        Guid sellerId,
        CreateSellerListingRequestDto? request,
        CancellationToken cancellationToken = default);

    Task<Result<PagedSellerListingsResponseDto>>
    GetForSellerAsync(
        Guid sellerId,
        SellerListingQueryDto? query,
        CancellationToken cancellationToken = default);

    Task<Result<SellerListingResponseDto>>
        GetByIdAsync(
            Guid sellerId,
            Guid listingId,
            CancellationToken cancellationToken = default);

    Task<Result<SellerListingResponseDto>>
    UpdatePriceAsync(
        Guid sellerId,
        Guid listingId,
        UpdateSellerListingPriceRequestDto? request,
        CancellationToken cancellationToken = default);

    Task<Result<SellerListingResponseDto>>
        ArchiveAsync(
            Guid sellerId,
            Guid listingId,
            ArchiveSellerListingRequestDto? request,
            CancellationToken cancellationToken = default);

}