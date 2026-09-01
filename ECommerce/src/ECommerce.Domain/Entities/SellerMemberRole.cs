using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class SellerMemberRole
{
    private SellerMemberRole()
    {
    }

    public SellerMemberRole(
        SellerMember sellerMember,
        SellerRole sellerRole)
    {
        ArgumentNullException.ThrowIfNull(sellerMember);
        ArgumentNullException.ThrowIfNull(sellerRole);

        if (sellerMember.SellerId != sellerRole.SellerId)
        {
            throw new InvalidOperationException(
                "A member cannot receive a role belonging to another seller.");
        }

        if (sellerMember.Status == SellerMemberStatus.Removed)
        {
            throw new InvalidOperationException(
                "A removed member cannot receive a role.");
        }

        if (!sellerRole.IsActive)
        {
            throw new InvalidOperationException(
                "An inactive role cannot be assigned.");
        }

        SellerId = sellerMember.SellerId;

        SellerMemberId = sellerMember.Id;
        SellerMember = sellerMember;

        SellerRoleId = sellerRole.Id;
        SellerRole = sellerRole;

        AssignedAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid SellerId { get; private set; }

    public Guid SellerMemberId { get; private set; }

    public SellerMember SellerMember { get; private set; } = null!;

    public Guid SellerRoleId { get; private set; }

    public SellerRole SellerRole { get; private set; } = null!;

    public DateTimeOffset AssignedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public bool IsActive { get; private set; }

    public void Revoke()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RevokedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Restore()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        RevokedAtUtc = null;
    }
}
