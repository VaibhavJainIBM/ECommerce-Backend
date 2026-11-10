namespace ECommerce.Product.Application.Catalog;

public sealed class AdminCatalogQueryDto
{
    public string? Search { get; init; }

    public string? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed record AdminCatalogVariantListItemDto(
    Guid VariantId,
    string Name,
    string VariantCode,
    string? Gtin,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record AdminCatalogProductListItemDto(
    Guid ProductId,
    string Title,
    string BrandName,
    string? Description,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyCollection<AdminCatalogVariantListItemDto> Variants);

public sealed record PagedAdminCatalogProductsResponseDto(
    IReadOnlyCollection<AdminCatalogProductListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record AdminCatalogPage(
    IReadOnlyCollection<AdminCatalogProductListItemDto> Items,
    int TotalCount);