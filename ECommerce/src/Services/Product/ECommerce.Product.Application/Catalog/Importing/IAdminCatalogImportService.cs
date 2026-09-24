using ECommerce.Product.Application.Common;

namespace ECommerce.Product.Application.Catalog.Importing;

public interface IAdminCatalogImportService
{
    Task<Result<CatalogImportResponseDto>> ImportAsync(
        Stream csvStream,
        bool activate,
        CancellationToken cancellationToken = default);
}
