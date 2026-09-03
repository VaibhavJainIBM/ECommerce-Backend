using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;

namespace ECommerce.Application.Catalog.Browsing;

public sealed class CatalogBrowsingService(ICatalogBrowsingRepository repository)
    : ICatalogBrowsingService
{
    public async Task<Result<PagedCatalogProductsResponseDto>> SearchAsync(
        CatalogQueryDto? query,
        CancellationToken cancellationToken = default)
    {
        query ??= new CatalogQueryDto();
        var errors = new List<Error>();
        if (query.Page < 1)
            errors.Add(new Error("catalog.page_invalid", "Page must be at least 1."));
        if (query.PageSize is < 1 or > 100)
            errors.Add(new Error("catalog.page_size_invalid", "Page size must be between 1 and 100."));

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        if (search?.Length > 100)
            errors.Add(new Error("catalog.search_too_long", "Search cannot exceed 100 characters."));

        var skip = ((long)query.Page - 1) * query.PageSize;
        if (skip > int.MaxValue)
            errors.Add(new Error("catalog.pagination_too_deep", "The requested page is too far into the results."));

        if (errors.Count > 0)
            return Result<PagedCatalogProductsResponseDto>.Failure(errors);

        var page = await repository.SearchAsync(search, (int)skip, query.PageSize, cancellationToken);
        return Result<PagedCatalogProductsResponseDto>.Success(new PagedCatalogProductsResponseDto(
            page.Items,
            query.Page,
            query.PageSize,
            page.TotalCount,
            (int)Math.Ceiling(page.TotalCount / (double)query.PageSize)));
    }

    public async Task<Result<CatalogProductResponseDto>> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
            return Result<CatalogProductResponseDto>.Failure(CatalogErrors.ProductNotFound(productId));

        var product = await repository.FindByIdAsync(productId, cancellationToken);
        return product is null
            ? Result<CatalogProductResponseDto>.Failure(CatalogErrors.ProductNotFound(productId))
            : Result<CatalogProductResponseDto>.Success(product);
    }
}
