using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Permission : AuditableEntity
{
    private Permission()
    {
    }

    public Permission(
        string code,
        string name,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedCode = code.Trim().ToLowerInvariant();

        if (normalizedCode.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException(
                "Permission code cannot contain spaces.",
                nameof(code));
        }

        Code = normalizedCode;
        Name = name.Trim();

        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<SellerRolePermission> RoleAssignments
    {
        get;
        private set;
    } = new List<SellerRolePermission>();

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
