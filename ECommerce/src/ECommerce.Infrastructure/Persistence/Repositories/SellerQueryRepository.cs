using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Sellers.Dtos;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class SellerQueryRepository(
    ECommerceDbContext dbContext)
    : ISellerQueryRepository
{
    public async Task<
        IReadOnlyCollection<MySellerResponseDto>>
        GetForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        var rows = await dbContext.SellerMembers
            .AsNoTracking()
            .Where(member =>
                member.UserId == userId &&
                member.Status !=
                    SellerMemberStatus.Removed)
            .OrderByDescending(member =>
                member.Seller.CreatedAtUtc)
            .ThenBy(member =>
                member.Seller.DisplayName)
            .Select(member => new
            {
                member.SellerId,
                member.Seller.DisplayName,
                member.Seller.LegalBusinessName,

                SellerStatus =
                    member.Seller.Status,

                SellerCreatedAtUtc =
                    member.Seller.CreatedAtUtc,

                member.Seller.ApprovedAtUtc,

                MemberId = member.Id,

                MemberStatus =
                    member.Status,

                member.JoinedAtUtc,

                Roles = member.RoleAssignments
                    .Where(assignment =>
                        assignment.IsActive &&
                        assignment.SellerRole.IsActive)
                    .OrderBy(assignment =>
                        assignment.SellerRole.NormalizedName)
                    .Select(assignment =>
                        assignment.SellerRole.Name)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new MySellerResponseDto(
                row.SellerId,
                row.DisplayName,
                row.LegalBusinessName,
                row.SellerStatus.ToString(),
                row.SellerCreatedAtUtc,
                row.ApprovedAtUtc,
                row.MemberId,
                row.MemberStatus.ToString(),
                row.JoinedAtUtc,
                row.Roles))
            .ToArray();
    }
}