namespace ECommerce.Application.Catalog.Browsing;

public sealed class CatalogQueryDto
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record CatalogVariantResponseDto(
    Guid VariantId,
    string Name,
    string VariantCode,
    string? Gtin);

public sealed record CatalogProductResponseDto(
    Guid ProductId,
    string Title,
    string BrandName,
    string? Description,
    IReadOnlyCollection<CatalogVariantResponseDto> Variants);

public sealed record PagedCatalogProductsResponseDto(
    IReadOnlyCollection<CatalogProductResponseDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CatalogProductPage(
    IReadOnlyCollection<CatalogProductResponseDto> Items,
    int TotalCount);
