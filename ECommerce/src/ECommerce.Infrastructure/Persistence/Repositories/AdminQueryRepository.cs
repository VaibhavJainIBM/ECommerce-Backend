using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Administration;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class AdminQueryRepository(
    ECommerceDbContext dbContext)
    : IAdminQueryRepository
{
    public async Task<AdminQueryPage<AdminSellerListItemDto>>
        GetSellersAsync(
            string? search,
            SellerStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        var query = dbContext.Sellers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(seller =>
                seller.DisplayName.Contains(search) ||
                seller.LegalBusinessName.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(seller =>
                seller.Status == status.Value);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var sellers = await query
            .OrderByDescending(seller => seller.CreatedAtUtc)
            .ThenBy(seller => seller.DisplayName)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);

        var items = sellers
            .Select(seller => new AdminSellerListItemDto(
                seller.Id,
                seller.DisplayName,
                seller.LegalBusinessName,
                seller.Status.ToString(),
                seller.ApprovedAtUtc,
                seller.CreatedAtUtc,
                seller.UpdatedAtUtc))
            .ToArray();

        return new AdminQueryPage<AdminSellerListItemDto>(
            items,
            totalCount);
    }

    public async Task<AdminQueryPage<AdminListingListItemDto>>
        GetListingsAsync(
            string? search,
            SellerListingStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        var query = dbContext.SellerListings
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(listing =>
                listing.Seller.DisplayName.Contains(search) ||
                listing.ProductVariant.Product.Title.Contains(search) ||
                listing.ProductVariant.Product.BrandName.Contains(search) ||
                listing.ProductVariant.Name.Contains(search) ||
                listing.ProductVariant.VariantCode.Contains(search) ||
                listing.SellerSku.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(listing =>
                listing.Status == status.Value);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var listings = await query
            .Include(listing => listing.Seller)
            .Include(listing => listing.ProductVariant)
            .ThenInclude(variant => variant.Product)
            .OrderByDescending(listing => listing.CreatedAtUtc)
            .ThenByDescending(listing => listing.Id)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);

        var items = listings
            .Select(listing => new AdminListingListItemDto(
                listing.Id,
                listing.SellerId,
                listing.Seller.DisplayName,
                listing.Seller.Status.ToString(),
                listing.ProductVariant.ProductId,
                listing.ProductVariant.Product.Title,
                listing.ProductVariant.Product.BrandName,
                listing.ProductVariantId,
                listing.ProductVariant.Name,
                listing.ProductVariant.VariantCode,
                listing.SellerSku,
                listing.Price.Amount,
                listing.Price.CurrencyCode,
                listing.Status.ToString(),
                Convert.ToBase64String(listing.RowVersion),
                listing.CreatedAtUtc))
            .ToArray();

        return new AdminQueryPage<AdminListingListItemDto>(
            items,
            totalCount);
    }

    public async Task<AdminQueryPage<AdminCatalogProductListItemDto>>
        GetCatalogProductsAsync(
            string? search,
            ProductStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(product =>
                product.Title.Contains(search) ||
                product.BrandName.Contains(search) ||
                product.Variants.Any(variant =>
                    variant.Name.Contains(search) ||
                    variant.VariantCode.Contains(search) ||
                    variant.Gtin != null &&
                    variant.Gtin.Contains(search)));
        }

        if (status.HasValue)
        {
            query = query.Where(product =>
                product.Status == status.Value);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var products = await query
            .Include(product => product.Variants)
            .OrderByDescending(product => product.CreatedAtUtc)
            .ThenBy(product => product.Title)
            .Skip(skip)
            .Take(take)
            .AsSplitQuery()
            .ToArrayAsync(cancellationToken);

        var items = products
            .Select(MapProduct)
            .ToArray();

        return new AdminQueryPage<AdminCatalogProductListItemDto>(
            items,
            totalCount);
    }

    private static AdminCatalogProductListItemDto MapProduct(
        Product product)
    {
        var variants = product.Variants
            .OrderBy(variant => variant.VariantCode)
            .ThenBy(variant => variant.Id)
            .Select(variant =>
                new AdminCatalogVariantListItemDto(
                    variant.Id,
                    variant.Name,
                    variant.VariantCode,
                    variant.Gtin,
                    variant.Status.ToString(),
                    variant.CreatedAtUtc))
            .ToArray();

        return new AdminCatalogProductListItemDto(
            product.Id,
            product.Title,
            product.BrandName,
            product.Description,
            product.Status.ToString(),
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            variants);
    }
}
