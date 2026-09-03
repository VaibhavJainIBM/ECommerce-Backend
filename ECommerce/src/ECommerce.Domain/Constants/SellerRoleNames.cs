namespace ECommerce.Domain.Constants;

public static class SellerRoleNames
{
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string WarehouseStaff = "WarehouseStaff";

    public static string? Canonical(string? name) => new[] { Owner, Manager, WarehouseStaff }
        .FirstOrDefault(role => string.Equals(role, name?.Trim(), StringComparison.OrdinalIgnoreCase));
}
