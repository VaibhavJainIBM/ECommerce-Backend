namespace ECommerce.Application.Catalog.Importing;

public sealed record CatalogImportedVariantDto(
    int CsvRowNumber,
    Guid VariantId,
    string VariantName,
    string VariantCode,
    string? Gtin,
    string Status);

public sealed record CatalogImportedProductDto(
    string ProductKey,
    Guid ProductId,
    string Status,
    IReadOnlyCollection<CatalogImportedVariantDto> Variants);

public sealed record CatalogImportResponseDto(
    int RowsProcessed,
    int ProductsCreated,
    int VariantsCreated,
    bool Activated,
    IReadOnlyCollection<CatalogImportedProductDto> Products);
