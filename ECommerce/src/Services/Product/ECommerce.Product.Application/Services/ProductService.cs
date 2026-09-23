using ECommerce.Product.Application.Abstractions;
using ECommerce.Product.Application.Contracts;
using ECommerce.Product.Domain.Entities;

namespace ECommerce.Product.Application.Services;

public sealed class ProductService(
    IProductRepository repository)
    : IProductService
{
    public async Task<IReadOnlyCollection<ProductResponse>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        var products =
            await repository.GetAllAsync(
                cancellationToken);

        return products
            .Select(Map)
            .ToArray();
    }

    public async Task<ProductResponse?>
        GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        var product =
            await repository.GetByIdAsync(
                id,
                cancellationToken);

        return product is null
            ? null
            : Map(product);
    }

    public async Task<ProductResponse>
        CreateAsync(
            CreateProductRequest request,
            CancellationToken cancellationToken = default)
    {
        var product =
            new Domain.Entities.Product(
                request.Title,
                request.BrandName,
                request.Description);

        await repository.AddAsync(
            product,
            cancellationToken);

        await repository.SaveChangesAsync(
            cancellationToken);

        return Map(product);
    }

    public async Task<ProductResponse?>
        AddVariantAsync(
            Guid productId,
            CreateVariantRequest request,
            CancellationToken cancellationToken = default)
    {
        var product =
            await repository.GetByIdAsync(
                productId,
                cancellationToken);

        if (product is null)
            return null;

        var variant =
            new ProductVariant(
                product.Id,
                request.Name,
                request.VariantCode,
                request.Gtin);

        product.Variants.Add(variant);

        await repository.SaveChangesAsync(
            cancellationToken);

        return Map(product);
    }

    private static ProductResponse Map(
        Domain.Entities.Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Title,
            product.BrandName,
            product.Description,
            product.Status.ToString(),
            product.Variants
                .Select(variant =>
                    new ProductVariantResponse(
                        variant.Id,
                        variant.Name,
                        variant.VariantCode,
                        variant.Gtin,
                        variant.Status.ToString()))
                .ToArray());
    }
}