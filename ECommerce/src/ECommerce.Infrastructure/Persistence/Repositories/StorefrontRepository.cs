using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Storefront;
using ECommerce.Domain.Entities;
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
                listing.ProductVariant.Product.Title.Contains(
                    normalizedSearch) ||
                listing.ProductVariant.Product.BrandName.Contains(
                    normalizedSearch) ||
                listing.ProductVariant.Name.Contains(
                    normalizedSearch) ||
                listing.Seller.DisplayName.Contains(
                    normalizedSearch));
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var pageQuery = query
            .OrderBy(listing => listing.ProductVariant.Product.Title)
            .ThenBy(listing => listing.Price.Amount)
            .ThenBy(listing => listing.Id)
            .Skip(skip)
            .Take(take);

        var items = await Project(pageQuery)
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
        var query = BuildActiveListingQuery()
            .Where(listing => listing.Id == listingId);

        return await Project(query)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<SellerListing>
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
                    ProductStatus.Active &&
                listing.InventoryItems.Any(inventory =>
                    inventory.Warehouse.Status == WarehouseStatus.Active &&
                    inventory.OnHandQuantity > inventory.ReservedQuantity));
    }

    // Keep projection last: filtering on a positional record constructor
    // is not reliably translatable by EF Core.
    private static IQueryable<StorefrontListingReadModel> Project(
        IQueryable<SellerListing> query)
    {
        return query.Select(listing =>
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
                            (long?)(
                                inventory.OnHandQuantity -
                                inventory.ReservedQuantity))
                        ?? 0L));
    }
}
