using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions.Persistence;

public interface ISellerOnboardingRepository
{
    Task CreateSellerWithOwnerAsync(
        Seller seller,
        SellerMember ownerMember,
        SellerRole ownerRole,
        SellerMemberRole ownerRoleAssignment,
        CancellationToken cancellationToken = default);
}