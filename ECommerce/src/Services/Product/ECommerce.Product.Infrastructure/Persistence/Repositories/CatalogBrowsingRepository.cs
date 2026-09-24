using ECommerce.Product.Application.Abstractions;
using ECommerce.Product.Application.Catalog.Browsing;
using ECommerce.Product.Domain.Entities;
using ECommerce.Product.Domain.Enums;
using ECommerce.Product.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ProductEntity = ECommerce.Product.Domain.Entities.Product;

namespace ECommerce.Product.Infrastructure.Persistence.Repositories;

public sealed class CatalogBrowsingRepository(
    ProductDbContext dbContext)
    : ICatalogBrowsingRepository
{
    public async Task<CatalogProductPage> SearchAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = ActiveProducts();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(product =>
                product.Title.Contains(term) ||
                product.BrandName.Contains(term) ||
                product.Variants.Any(variant =>
                    variant.Status ==
                        ProductVariantStatus.Active &&
                    (
                        variant.Name.Contains(term) ||
                        variant.VariantCode.Contains(term) ||
                        (
                            variant.Gtin != null &&
                            variant.Gtin.Contains(term)
                        )
                    )));
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var pageQuery =
            query
                .OrderBy(product => product.Title)
                .ThenBy(product => product.Id)
                .Skip(skip)
                .Take(take);

        var items =
            await Project(pageQuery)
                .ToArrayAsync(
                    cancellationToken);

        return new CatalogProductPage(
            items,
            totalCount);
    }

    public Task<CatalogProductResponseDto?>
        FindByIdAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
    {
        return Project(
                ActiveProducts()
                    .Where(product =>
                        product.Id == productId))
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    private IQueryable<ProductEntity> ActiveProducts()
    {
        return dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.Status ==
                    ProductStatus.Active);
    }

    private static IQueryable<CatalogProductResponseDto>
        Project(
            IQueryable<ProductEntity> query)
    {
        return query.Select(
            product =>
                new CatalogProductResponseDto(
                    product.Id,
                    product.Title,
                    product.BrandName,
                    product.Description,
                    product.Variants
                        .Where(variant =>
                            variant.Status ==
                                ProductVariantStatus.Active)
                        .OrderBy(variant =>
                            variant.VariantCode)
                        .ThenBy(variant =>
                            variant.Id)
                        .Select(variant =>
                            new CatalogVariantResponseDto(
                                variant.Id,
                                variant.Name,
                                variant.VariantCode,
                                variant.Gtin))
                        .ToArray()));
    }
}