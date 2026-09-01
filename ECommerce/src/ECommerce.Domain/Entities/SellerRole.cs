using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class SellerRole : AuditableEntity
{
    private SellerRole()
    {
    }

    public SellerRole(
        Guid sellerId,
        string name,
        string? description = null,
        bool isBuiltIn = false)
    {
        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Seller ID is required.",
                nameof(sellerId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        SellerId = sellerId;
        Name = name.Trim();
        NormalizedName = name.Trim().ToUpperInvariant();

        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        IsBuiltIn = isBuiltIn;
        IsActive = true;
    }

    public Guid SellerId { get; private set; }

    public Seller Seller { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsBuiltIn { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<SellerMemberRole> MemberAssignments
    {
        get;
        private set;
    } = new List<SellerMemberRole>();

    public ICollection<SellerRolePermission> PermissionAssignments
    {
        get;
        private set;
    } = new List<SellerRolePermission>();

    public void UpdateDetails(
        string name,
        string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        NormalizedName = name.Trim().ToUpperInvariant();

        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        MarkUpdated();
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;

        MarkUpdated();
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;

        MarkUpdated();
    }
}
