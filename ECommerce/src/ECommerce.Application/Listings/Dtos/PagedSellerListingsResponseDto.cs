namespace ECommerce.Application.Listings.Dtos;

public sealed record PagedSellerListingsResponseDto(
    IReadOnlyCollection<SellerListingResponseDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);