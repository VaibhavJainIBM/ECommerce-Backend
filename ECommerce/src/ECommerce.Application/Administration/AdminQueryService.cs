using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Administration;

public sealed class AdminQueryService(
    IAdminQueryRepository repository)
    : IAdminQueryService
{
    public async Task<Result<PagedAdminSellersResponseDto>>
        GetSellersAsync(
            AdminQueryDto? query,
            CancellationToken cancellationToken = default)
    {
        var prepared = Prepare<SellerStatus>(query);

        if (prepared.Errors.Count > 0)
        {
            return Result<PagedAdminSellersResponseDto>
                .Failure(prepared.Errors);
        }

        var page = await repository.GetSellersAsync(
            prepared.Search,
            prepared.Status,
            prepared.Skip,
            prepared.PageSize,
            cancellationToken);

        return Result<PagedAdminSellersResponseDto>.Success(
            new PagedAdminSellersResponseDto(
                page.Items,
                prepared.Page,
                prepared.PageSize,
                page.TotalCount,
                TotalPages(page.TotalCount, prepared.PageSize)));
    }

    public async Task<Result<PagedAdminListingsResponseDto>>
        GetListingsAsync(
            AdminQueryDto? query,
            CancellationToken cancellationToken = default)
    {
        var prepared = Prepare<SellerListingStatus>(query);

        if (prepared.Errors.Count > 0)
        {
            return Result<PagedAdminListingsResponseDto>
                .Failure(prepared.Errors);
        }

        var page = await repository.GetListingsAsync(
            prepared.Search,
            prepared.Status,
            prepared.Skip,
            prepared.PageSize,
            cancellationToken);

        return Result<PagedAdminListingsResponseDto>.Success(
            new PagedAdminListingsResponseDto(
                page.Items,
                prepared.Page,
                prepared.PageSize,
                page.TotalCount,
                TotalPages(page.TotalCount, prepared.PageSize)));
    }

    public async Task<Result<PagedAdminCatalogProductsResponseDto>>
        GetCatalogProductsAsync(
            AdminQueryDto? query,
            CancellationToken cancellationToken = default)
    {
        var prepared = Prepare<ProductStatus>(query);

        if (prepared.Errors.Count > 0)
        {
            return Result<PagedAdminCatalogProductsResponseDto>
                .Failure(prepared.Errors);
        }

        var page = await repository.GetCatalogProductsAsync(
            prepared.Search,
            prepared.Status,
            prepared.Skip,
            prepared.PageSize,
            cancellationToken);

        return Result<PagedAdminCatalogProductsResponseDto>.Success(
            new PagedAdminCatalogProductsResponseDto(
                page.Items,
                prepared.Page,
                prepared.PageSize,
                page.TotalCount,
                TotalPages(page.TotalCount, prepared.PageSize)));
    }

    private static PreparedQuery<TStatus> Prepare<TStatus>(
        AdminQueryDto? query)
        where TStatus : struct, Enum
    {
        query ??= new AdminQueryDto();

        var errors = new List<Error>();
        var search = string.IsNullOrWhiteSpace(query.Search)
            ? null
            : query.Search.Trim();

        if (search?.Length > 100)
        {
            errors.Add(AdminQueryErrors.SearchTooLong);
        }

        if (query.Page < 1)
        {
            errors.Add(AdminQueryErrors.PageInvalid);
        }

        if (query.PageSize is < 1 or > 100)
        {
            errors.Add(AdminQueryErrors.PageSizeInvalid);
        }

        TStatus? status = null;

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var suppliedStatus = query.Status.Trim();

            if (!Enum.TryParse<TStatus>(
                    suppliedStatus,
                    true,
                    out var parsedStatus) ||
                !Enum.IsDefined(parsedStatus))
            {
                errors.Add(
                    AdminQueryErrors.InvalidStatus(
                        suppliedStatus));
            }
            else
            {
                status = parsedStatus;
            }
        }

        var skipAsLong = query.Page < 1
            ? 0
            : ((long)query.Page - 1) * query.PageSize;

        if (skipAsLong > int.MaxValue)
        {
            errors.Add(AdminQueryErrors.PaginationTooDeep);
        }

        return new PreparedQuery<TStatus>(
            search,
            status,
            query.Page,
            query.PageSize,
            skipAsLong > int.MaxValue ? 0 : (int)skipAsLong,
            errors);
    }

    private static int TotalPages(int totalCount, int pageSize)
    {
        return totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    private sealed record PreparedQuery<TStatus>(
        string? Search,
        TStatus? Status,
        int Page,
        int PageSize,
        int Skip,
        IReadOnlyCollection<Error> Errors)
        where TStatus : struct, Enum;
}
