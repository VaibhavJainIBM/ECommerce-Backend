using ECommerce.Application.Common;

namespace ECommerce.Application.Administration;

public sealed class AdminQueryDto
{
    public string? Search { get; init; }

    public string? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed record AdminSellerListItemDto(
    Guid SellerId,
    string DisplayName,
    string LegalBusinessName,
    string Status,
    DateTimeOffset? ApprovedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record PagedAdminSellersResponseDto(
    IReadOnlyCollection<AdminSellerListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record AdminListingListItemDto(
    Guid ListingId,
    Guid SellerId,
    string SellerDisplayName,
    string SellerStatus,
    Guid ProductId,
    string ProductTitle,
    string BrandName,
    Guid ProductVariantId,
    string VariantName,
    string VariantCode,
    string SellerSku,
    decimal PriceAmount,
    string CurrencyCode,
    string Status,
    string RowVersion,
    DateTimeOffset CreatedAtUtc);

public sealed record PagedAdminListingsResponseDto(
    IReadOnlyCollection<AdminListingListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

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

public sealed record AdminQueryPage<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount);

public interface IAdminQueryService
{
    Task<Result<PagedAdminSellersResponseDto>>
        GetSellersAsync(
            AdminQueryDto? query,
            CancellationToken cancellationToken = default);

    Task<Result<PagedAdminListingsResponseDto>>
        GetListingsAsync(
            AdminQueryDto? query,
            CancellationToken cancellationToken = default);

    Task<Result<PagedAdminCatalogProductsResponseDto>>
        GetCatalogProductsAsync(
            AdminQueryDto? query,
            CancellationToken cancellationToken = default);
}
