using ECommerce.Application.Common;

namespace ECommerce.Application.Catalog.Importing;

public interface IAdminCatalogImportService
{
    Task<Result<CatalogImportResponseDto>> ImportAsync(
        Stream csvStream,
        bool activate,
        CancellationToken cancellationToken = default);
}
