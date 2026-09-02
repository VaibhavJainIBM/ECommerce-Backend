namespace ECommerce.Application.Sellers.Dtos;

public sealed record SellerLifecycleResponseDto(
    Guid SellerId,
    string DisplayName,
    string LegalBusinessName,
    string Status,
    DateTimeOffset? ApprovedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);