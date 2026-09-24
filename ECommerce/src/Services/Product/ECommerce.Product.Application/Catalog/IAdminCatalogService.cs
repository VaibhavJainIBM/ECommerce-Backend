using ECommerce.Product.Application.Catalog.Dtos;
using ECommerce.Product.Application.Common;

namespace ECommerce.Product.Application.Catalog;

public interface IAdminCatalogService
{
    Task<Result<CreateProductResponseDto>>
        CreateProductAsync(
            CreateProductRequestDto? request,
            CancellationToken cancellationToken = default);

    Task<Result<CreateProductResponseDto>>
        ActivateProductAsync(
            Guid productId,
            CancellationToken cancellationToken = default);
}