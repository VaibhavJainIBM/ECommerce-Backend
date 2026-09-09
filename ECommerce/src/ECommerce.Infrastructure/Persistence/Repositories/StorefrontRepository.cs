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
        StorefrontSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = BuildActiveListingQuery();

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var normalizedSearch = criteria.Search.Trim();

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


        if (!string.IsNullOrWhiteSpace(criteria.Brand))
        {
            var normalizedBrand = criteria.Brand.Trim();

            query = query.Where(listing =>
                listing.ProductVariant.Product.BrandName.Contains(
                    normalizedBrand));
        }

        if (criteria.MinPrice.HasValue)
        {
            query = query.Where(listing =>
                listing.Price.Amount >= criteria.MinPrice.Value);
        }

        if (criteria.MaxPrice.HasValue)
        {
            query = query.Where(listing =>
                listing.Price.Amount <= criteria.MaxPrice.Value);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var pageQuery = ApplySort(query, criteria.Sort)
            .Skip(criteria.Skip)
            .Take(criteria.Take);

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

    private static IOrderedQueryable<SellerListing> ApplySort(
        IQueryable<SellerListing> query,
        StorefrontSort sort)
    {
        return sort switch
        {
            StorefrontSort.NameDescending => query
                .OrderByDescending(listing =>
                    listing.ProductVariant.Product.Title)
                .ThenBy(listing => listing.Price.Amount)
                .ThenBy(listing => listing.Id),
            StorefrontSort.PriceAscending => query
                .OrderBy(listing => listing.Price.Amount)
                .ThenBy(listing =>
                    listing.ProductVariant.Product.Title)
                .ThenBy(listing => listing.Id),
            StorefrontSort.PriceDescending => query
                .OrderByDescending(listing => listing.Price.Amount)
                .ThenBy(listing =>
                    listing.ProductVariant.Product.Title)
                .ThenBy(listing => listing.Id),
            _ => query
                .OrderBy(listing =>
                    listing.ProductVariant.Product.Title)
                .ThenBy(listing => listing.Price.Amount)
                .ThenBy(listing => listing.Id)
        };
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
