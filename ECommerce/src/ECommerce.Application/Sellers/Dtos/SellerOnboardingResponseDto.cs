namespace ECommerce.Application.Sellers.Dtos;

public sealed record SellerOnboardingResponseDto(
    Guid SellerId,
    string DisplayName,
    string LegalBusinessName,
    string SellerStatus,
    Guid OwnerMemberId,
    string MemberStatus,
    Guid OwnerRoleId,
    string Role,
    DateTimeOffset CreatedAtUtc);