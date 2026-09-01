namespace ECommerce.Application.Sellers.Dtos;

public sealed record MySellerResponseDto(
    Guid SellerId,
    string DisplayName,
    string LegalBusinessName,
    string SellerStatus,
    DateTimeOffset SellerCreatedAtUtc,
    DateTimeOffset? ApprovedAtUtc,
    Guid MemberId,
    string MemberStatus,
    DateTimeOffset? JoinedAtUtc,
    IReadOnlyCollection<string> Roles);