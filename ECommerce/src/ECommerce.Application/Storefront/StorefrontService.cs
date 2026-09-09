using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;

namespace ECommerce.Application.Storefront;

public sealed class StorefrontService(
    IStorefrontRepository repository)
    : IStorefrontService
{
    public async Task<
        Result<PagedStorefrontListingsResponseDto>>
        SearchAsync(
            StorefrontQueryDto? query,
            CancellationToken cancellationToken = default)
    {
        query ??= new StorefrontQueryDto();

        var errors = new List<Error>();

        if (query.Page < 1)
        {
            errors.Add(StorefrontErrors.PageInvalid);
        }

        if (query.PageSize is < 1 or > 100)
        {
            errors.Add(StorefrontErrors.PageSizeInvalid);
        }

        var search = string.IsNullOrWhiteSpace(query.Search)
            ? null
            : query.Search.Trim();

        var brand = string.IsNullOrWhiteSpace(query.Brand)
            ? null
            : query.Brand.Trim();

        if (search?.Length > 100)
        {
            errors.Add(StorefrontErrors.SearchTooLong);
        }

        if (brand?.Length > 150)
        {
            errors.Add(StorefrontErrors.BrandTooLong);
        }

        if (query.MinPrice < 0 || query.MaxPrice < 0)
        {
            errors.Add(StorefrontErrors.PriceInvalid);
        }

        if (query.MinPrice.HasValue &&
            query.MaxPrice.HasValue &&
            query.MinPrice > query.MaxPrice)
        {
            errors.Add(StorefrontErrors.PriceRangeInvalid);
        }

        var sort = ParseSort(query.Sort);

        if (sort is null)
        {
            errors.Add(StorefrontErrors.SortInvalid);
        }

        var skipAsLong =
            ((long)query.Page - 1) * query.PageSize;

        if (skipAsLong > int.MaxValue)
        {
            errors.Add(StorefrontErrors.PaginationTooDeep);
        }

        if (errors.Count > 0)
        {
            return Result<
                PagedStorefrontListingsResponseDto>
                .Failure(errors);
        }

        var criteria = new StorefrontSearchCriteria(
            search,
            brand,
            query.MinPrice,
            query.MaxPrice,
            sort!.Value,
            (int)skipAsLong,
            query.PageSize);

        var page = await repository.SearchAsync(
            criteria,
            cancellationToken);

        var items = page.Items
            .Select(Map)
            .ToArray();

        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                page.TotalCount /
                (double)query.PageSize);

        var response =
            new PagedStorefrontListingsResponseDto(
                items,
                query.Page,
                query.PageSize,
                page.TotalCount,
                totalPages);

        return Result<
            PagedStorefrontListingsResponseDto>
            .Success(response);
    }

    public async Task<Result<StorefrontListingResponseDto>>
        GetByIdAsync(
            Guid listingId,
            CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty)
        {
            return Result<StorefrontListingResponseDto>
                .Failure(
                    StorefrontErrors.ListingNotFound(
                        listingId));
        }

        var listing = await repository.FindByIdAsync(
            listingId,
            cancellationToken);

        if (listing is null)
        {
            return Result<StorefrontListingResponseDto>
                .Failure(
                    StorefrontErrors.ListingNotFound(
                        listingId));
        }

        return Result<StorefrontListingResponseDto>
            .Success(Map(listing));
    }

    private static StorefrontListingResponseDto Map(
        StorefrontListingReadModel listing)
    {
        return new StorefrontListingResponseDto(
            listing.ListingId,
            listing.SellerId,
            listing.SellerDisplayName,
            listing.ProductId,
            listing.ProductTitle,
            listing.BrandName,
            listing.Description,
            listing.ProductVariantId,
            listing.VariantName,
            listing.VariantCode,
            listing.SellerSku,
            listing.PriceAmount,
            listing.CurrencyCode,
            listing.AvailableQuantity);
    }

    private static StorefrontSort? ParseSort(string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            null or "" or StorefrontSortNames.NameAscending =>
                StorefrontSort.NameAscending,
            StorefrontSortNames.NameDescending =>
                StorefrontSort.NameDescending,
            StorefrontSortNames.PriceAscending =>
                StorefrontSort.PriceAscending,
            StorefrontSortNames.PriceDescending =>
                StorefrontSort.PriceDescending,
            _ => null
        };
    }
}
