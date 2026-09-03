using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Constants;

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
        dbContext.SellerRoles.Add(new SellerRole(seller.Id, SellerRoleNames.Manager, "Manage seller operations.", true));
        dbContext.SellerRoles.Add(new SellerRole(seller.Id, SellerRoleNames.WarehouseStaff, "Manage assigned warehouse inventory.", true));
        dbContext.SellerMemberRoles.Add(
            ownerRoleAssignment);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
