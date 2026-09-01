using ECommerce.Application.Catalog.Dtos;
using ECommerce.Application.Common;

namespace ECommerce.Application.Catalog;

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