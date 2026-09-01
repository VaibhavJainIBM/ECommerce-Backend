namespace ECommerce.Domain.Entities;

public sealed class SellerRolePermission
{
    private SellerRolePermission()
    {
    }

    public SellerRolePermission(
        SellerRole sellerRole,
        Permission permission)
    {
        ArgumentNullException.ThrowIfNull(sellerRole);
        ArgumentNullException.ThrowIfNull(permission);

        if (!sellerRole.IsActive)
        {
            throw new InvalidOperationException(
                "Permissions cannot be granted to an inactive role.");
        }

        if (!permission.IsActive)
        {
            throw new InvalidOperationException(
                "An inactive permission cannot be granted.");
        }

        SellerId = sellerRole.SellerId;

        SellerRoleId = sellerRole.Id;
        SellerRole = sellerRole;

        PermissionId = permission.Id;
        Permission = permission;

        GrantedAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid SellerId { get; private set; }

    public Guid SellerRoleId { get; private set; }

    public SellerRole SellerRole { get; private set; } = null!;

    public Guid PermissionId { get; private set; }

    public Permission Permission { get; private set; } = null!;

    public DateTimeOffset GrantedAtUtc { get; private set; }

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
