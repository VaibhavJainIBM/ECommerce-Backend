namespace ECommerce.Application.Abstractions.Authorization;

public interface ISellerAccessReader
{
    Task<SellerAccessSnapshot?>
        FindActiveMembershipAsync(
            Guid userId,
            Guid sellerId,
            CancellationToken cancellationToken = default);
}

public sealed record SellerAccessSnapshot(
    Guid SellerMemberId,
    IReadOnlyCollection<string> ActiveNormalizedRoles);