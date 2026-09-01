using ECommerce.Domain.Common;
namespace ECommerce.Domain.Entities;

public sealed class WarehouseAssignment : Entity
{
    private WarehouseAssignment()
    {
    }

    public WarehouseAssignment(
        SellerMember sellerMember,
        Warehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(sellerMember);
        ArgumentNullException.ThrowIfNull(warehouse);

        if (sellerMember.SellerId != warehouse.SellerId)
        {
            throw new InvalidOperationException(
                "A member cannot be assigned to another seller's warehouse.");
        }

        SellerId = sellerMember.SellerId;

        SellerMemberId = sellerMember.Id;
        SellerMember = sellerMember;

        WarehouseId = warehouse.Id;
        Warehouse = warehouse;

        AssignedAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid SellerId { get; private set; }

    public Guid SellerMemberId { get; private set; }

    public SellerMember SellerMember { get; private set; } = null!;

    public Guid WarehouseId { get; private set; }

    public Warehouse Warehouse { get; private set; } = null!;

    public DateTimeOffset AssignedAtUtc { get; private set; }

    public DateTimeOffset? RemovedAtUtc { get; private set; }

    public bool IsActive { get; private set; }

    public void Remove()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RemovedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Restore()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        RemovedAtUtc = null;
    }
}