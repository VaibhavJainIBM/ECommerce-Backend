using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public sealed class Warehouse : AuditableEntity
{
    private Warehouse()
    {
    }

    public Warehouse(
        Guid sellerId,
        string name,
        string code,
        Address address)
    {
        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Seller ID is required.",
                nameof(sellerId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        SellerId = sellerId;
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Address = address
            ?? throw new ArgumentNullException(nameof(address));

        Status = WarehouseStatus.Draft;
    }

    public Guid SellerId { get; private set; }

    public Seller Seller { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public Address Address { get; private set; } = null!;

    public WarehouseStatus Status { get; private set; }

    public ICollection<WarehouseAssignment> StaffAssignments
    {
        get;
        private set;
    } = new List<WarehouseAssignment>();

    public void UpdateDetails(
        string name,
        Address address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Address = address
            ?? throw new ArgumentNullException(nameof(address));

        MarkUpdated();
    }

    public void Activate()
    {
        if (Status == WarehouseStatus.Active)
        {
            return;
        }

        Status = WarehouseStatus.Active;

        MarkUpdated();
    }

    public void TemporarilyClose()
    {
        if (Status != WarehouseStatus.Active)
        {
            throw new InvalidOperationException(
                "Only an active warehouse can be temporarily closed.");
        }

        Status = WarehouseStatus.TemporarilyClosed;

        MarkUpdated();
    }

    public void Deactivate()
    {
        if (Status == WarehouseStatus.Inactive)
        {
            return;
        }

        Status = WarehouseStatus.Inactive;

        MarkUpdated();
    }
}
