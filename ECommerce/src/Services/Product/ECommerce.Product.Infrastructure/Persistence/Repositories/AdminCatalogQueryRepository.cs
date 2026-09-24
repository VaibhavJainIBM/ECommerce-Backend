using ECommerce.Product.Application.Abstractions;
using ECommerce.Product.Application.Catalog;
using ECommerce.Product.Domain.Enums;
using Microsoft.EntityFrameworkCore;

using ProductEntity =
    ECommerce.Product.Domain.Entities.Product;

namespace ECommerce.Product.Infrastructure.Persistence.Repositories;

public sealed class AdminCatalogQueryRepository(
    ProductDbContext dbContext)
    : IAdminCatalogQueryRepository
{
    public async Task<AdminCatalogPage>
        GetProductsAsync(
            string? search,
            ProductStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        var query =
            dbContext.Products
                .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(product =>
                product.Title.Contains(search) ||
                product.BrandName.Contains(search) ||
                product.Variants.Any(variant =>
                    variant.Name.Contains(search) ||
                    variant.VariantCode.Contains(search) ||
                    (
                        variant.Gtin != null &&
                        variant.Gtin.Contains(search)
                    )));
        }

        if (status.HasValue)
        {
            query = query.Where(product =>
                product.Status == status.Value);
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var products =
            await query
                .Include(product => product.Variants)
                .OrderByDescending(
                    product => product.CreatedAtUtc)
                .ThenBy(product => product.Title)
                .Skip(skip)
                .Take(take)
                .AsSplitQuery()
                .ToArrayAsync(
                    cancellationToken);

        var items =
            products
                .Select(Map)
                .ToArray();

        return new AdminCatalogPage(
            items,
            totalCount);
    }

    private static AdminCatalogProductListItemDto Map(
        ProductEntity product)
    {
        var variants =
            product.Variants
                .OrderBy(
                    variant => variant.VariantCode)
                .ThenBy(
                    variant => variant.Id)
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