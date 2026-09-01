using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class SellerOnboardingRepository(
    ECommerceDbContext dbContext)
    : ISellerOnboardingRepository
{
    public async Task CreateSellerWithOwnerAsync(
        Seller seller,
        SellerMember ownerMember,
        SellerRole ownerRole,
        SellerMemberRole ownerRoleAssignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seller);
        ArgumentNullException.ThrowIfNull(ownerMember);
        ArgumentNullException.ThrowIfNull(ownerRole);
        ArgumentNullException.ThrowIfNull(
            ownerRoleAssignment);

        dbContext.Sellers.Add(seller);
        dbContext.SellerMembers.Add(ownerMember);
        dbContext.SellerRoles.Add(ownerRole);
        dbContext.SellerMemberRoles.Add(
            ownerRoleAssignment);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}