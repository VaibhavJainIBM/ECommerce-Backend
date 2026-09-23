using ECommerce.Product.Application.Contracts;

namespace ECommerce.Product.Application.Services;

public interface IProductService
{
    Task<IReadOnlyCollection<ProductResponse>>
        GetAllAsync(
            CancellationToken cancellationToken = default);

    Task<ProductResponse?>
        GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

    Task<ProductResponse>
        CreateAsync(
            CreateProductRequest request,
            CancellationToken cancellationToken = default);

    Task<ProductResponse?>
        AddVariantAsync(
            Guid productId,
            CreateVariantRequest request,
            CancellationToken cancellationToken = default);
}