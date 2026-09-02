using ECommerce.Application.Listings.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Abstractions.Persistence;

public interface ISellerListingRepository
{
    Task<SellerStatus?> GetSellerStatusAsync(
        Guid sellerId,
        CancellationToken cancellationToken = default);

    Task<SellerListingVariantSnapshot?> GetVariantAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<SellerListingCreateOutcome> TryCreateAsync(
        SellerListing listing,
        CancellationToken cancellationToken = default);

    Task<SellerListingPage> GetForSellerAsync(
    Guid sellerId,
    SellerListingStatus? status,
    int skip,
    int take,
    CancellationToken cancellationToken = default);

    Task<SellerListingReadModel?> FindByIdAsync(
        Guid sellerId,
        Guid listingId,
        CancellationToken cancellationToken = default);
}