using ECommerce.Application.Abstractions.Authorization;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class SellerAccessReader(
    ECommerceDbContext dbContext)
    : ISellerAccessReader
{
    public async Task<SellerAccessSnapshot?>
        FindActiveMembershipAsync(
            Guid userId,
            Guid sellerId,
            CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty ||
            sellerId == Guid.Empty)
        {
            return null;
        }

        // A valid but unexpired JWT must not keep a disabled account's seller access alive.
        if (!await dbContext.Users.AnyAsync(
                user => user.Id == userId && user.IsActive, cancellationToken))
        {
            return null;
        }

        var memberId = await dbContext.SellerMembers
            .AsNoTracking()
            .Where(member =>
                member.SellerId == sellerId &&
                member.UserId == userId &&
                member.Status ==
                    SellerMemberStatus.Active)
            .Select(member => (Guid?)member.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!memberId.HasValue)
        {
            return null;
        }

        var activeRoles =
            await dbContext.SellerMemberRoles
                .AsNoTracking()
                .Where(assignment =>
                    assignment.SellerId == sellerId &&
                    assignment.SellerMemberId ==
                        memberId.Value &&
                    assignment.IsActive &&
                    assignment.SellerRole.IsActive)
                .Select(assignment =>
                    assignment.SellerRole.NormalizedName)
                .Distinct()
                .ToArrayAsync(cancellationToken);

        return new SellerAccessSnapshot(
            memberId.Value,
            activeRoles);
    }
}
