using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Storefront;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class StorefrontRepository(
    ECommerceDbContext dbContext)
    : IStorefrontRepository
{
    public async Task<StorefrontListingPage> SearchAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = BuildActiveListingQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();

            query = query.Where(listing =>
                listing.ProductTitle.Contains(
                    normalizedSearch) ||
                listing.BrandName.Contains(
                    normalizedSearch) ||
                listing.VariantName.Contains(
                    normalizedSearch) ||
                listing.SellerDisplayName.Contains(
                    normalizedSearch));
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderBy(listing => listing.ProductTitle)
            .ThenBy(listing => listing.PriceAmount)
            .ThenBy(listing => listing.ListingId)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);

        return new StorefrontListingPage(
            items,
            totalCount);
    }

    public async Task<StorefrontListingReadModel?>
        FindByIdAsync(
            Guid listingId,
            CancellationToken cancellationToken = default)
    {
        return await BuildActiveListingQuery()
            .SingleOrDefaultAsync(
                listing => listing.ListingId == listingId,
                cancellationToken);
    }

    private IQueryable<StorefrontListingReadModel>
        BuildActiveListingQuery()
    {
        return dbContext.SellerListings
            .AsNoTracking()
            .Where(listing =>
                listing.Status ==
                    SellerListingStatus.Active &&
                listing.Seller.Status ==
                    SellerStatus.Active &&
                listing.ProductVariant.Status ==
                    ProductVariantStatus.Active &&
                listing.ProductVariant.Product.Status ==
                    ProductStatus.Active)
            .Select(listing =>
                new StorefrontListingReadModel(
                    listing.Id,
                    listing.SellerId,
                    listing.Seller.DisplayName,
                    listing.ProductVariant.ProductId,
                    listing.ProductVariant.Product.Title,
                    listing.ProductVariant.Product.BrandName,
                    listing.ProductVariant.Product.Description,
                    listing.ProductVariantId,
                    listing.ProductVariant.Name,
                    listing.ProductVariant.VariantCode,
                    listing.SellerSku,
                    listing.Price.Amount,
                    listing.Price.CurrencyCode,
                    listing.InventoryItems
                        .Where(inventory =>
                            inventory.Warehouse.Status ==
                                WarehouseStatus.Active)
                        .Sum(inventory =>
                            (int?)(
                                inventory.OnHandQuantity -
                                inventory.ReservedQuantity))
                        ?? 0))
            .Where(listing =>
                listing.AvailableQuantity > 0);
    }
}