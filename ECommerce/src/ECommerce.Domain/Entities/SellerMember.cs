using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class SellerMember : AuditableEntity
{
    private SellerMember()
    {
    }

    public SellerMember(
        Guid sellerId,
        Guid userId)
    {
        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Seller ID is required.",
                nameof(sellerId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        SellerId = sellerId;
        UserId = userId;
        Status = SellerMemberStatus.Invited;
        InvitedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid SellerId { get; private set; }

    public Seller Seller { get; private set; } = null!;

    public Guid UserId { get; private set; }

    public SellerMemberStatus Status { get; private set; }

    public DateTimeOffset InvitedAtUtc { get; private set; }

    public DateTimeOffset? JoinedAtUtc { get; private set; }

    public ICollection<WarehouseAssignment> WarehouseAssignments
    {
        get;
        private set;
    } = new List<WarehouseAssignment>();

    public ICollection<SellerMemberRole> RoleAssignments
    {
        get;
        private set;
    } = new List<SellerMemberRole>();

    public void Activate()
    {
        if (Status != SellerMemberStatus.Invited)
        {
            throw new InvalidOperationException(
                "Only an invited member can be activated.");
        }

        Status = SellerMemberStatus.Active;
        JoinedAtUtc = DateTimeOffset.UtcNow;

        MarkUpdated();
    }

    public void Suspend()
    {
        if (Status != SellerMemberStatus.Active)
        {
            throw new InvalidOperationException(
                "Only an active member can be suspended.");
        }

        Status = SellerMemberStatus.Suspended;

        MarkUpdated();
    }

    public void Reactivate()
    {
        if (Status != SellerMemberStatus.Suspended)
        {
            throw new InvalidOperationException(
                "Only a suspended member can be reactivated.");
        }

        Status = SellerMemberStatus.Active;

        MarkUpdated();
    }

    public void Remove()
    {
        if (Status == SellerMemberStatus.Removed)
        {
            return;
        }

        Status = SellerMemberStatus.Removed;

        MarkUpdated();
    }
}
