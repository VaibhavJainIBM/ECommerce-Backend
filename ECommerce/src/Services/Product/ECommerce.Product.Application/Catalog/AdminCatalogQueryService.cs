using ECommerce.Product.Application.Abstractions;
using ECommerce.Product.Application.Common;
using ECommerce.Product.Domain.Enums;

namespace ECommerce.Product.Application.Catalog;

public sealed class AdminCatalogQueryService(
    IAdminCatalogQueryRepository repository)
    : IAdminCatalogQueryService
{
    public async Task<Result<PagedAdminCatalogProductsResponseDto>>
        GetProductsAsync(
            AdminCatalogQueryDto? query,
            CancellationToken cancellationToken = default)
    {
        query ??= new AdminCatalogQueryDto();

        var errors = new List<Error>();

        if (query.Page < 1)
        {
            errors.Add(new Error(
                "AdminQuery.PageInvalid",
                "Page must be at least 1."));
        }

        if (query.PageSize is < 1 or > 100)
        {
            errors.Add(new Error(
                "AdminQuery.PageSizeInvalid",
                "Page size must be between 1 and 100."));
        }

        var search =
            string.IsNullOrWhiteSpace(query.Search)
                ? null
                : query.Search.Trim();

        if (search?.Length > 100)
        {
            errors.Add(new Error(
                "AdminQuery.SearchTooLong",
                "Search cannot exceed 100 characters."));
        }

        ProductStatus? status = null;

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var suppliedStatus = query.Status.Trim();

            if (!Enum.TryParse<ProductStatus>(
                    suppliedStatus,
                    true,
                    out var parsedStatus) ||
                !Enum.IsDefined(parsedStatus))
            {
                errors.Add(new Error(
                    "AdminQuery.InvalidStatus",
                    $"'{suppliedStatus}' is not a valid product status."));
            }
            else
            {
                status = parsedStatus;
            }
        }

        if (errors.Count > 0)
        {
            return Result<PagedAdminCatalogProductsResponseDto>
                .Failure(errors);
        }

        var skip =
            (query.Page - 1) * query.PageSize;

        var page =
            await repository.GetProductsAsync(
                search,
                status,
                skip,
                query.PageSize,
                cancellationToken);

        var totalPages =
            page.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    page.TotalCount /
                    (double)query.PageSize);

        return Result<PagedAdminCatalogProductsResponseDto>
            .Success(
                new PagedAdminCatalogProductsResponseDto(
                    page.Items,
                    query.Page,
                    query.PageSize,
                    page.TotalCount,
                    totalPages));
    }
}