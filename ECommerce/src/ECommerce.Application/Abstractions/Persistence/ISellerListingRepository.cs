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
}