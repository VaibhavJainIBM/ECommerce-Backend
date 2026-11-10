namespace ECommerce.User.Domain.Authorization;

/// <summary>
/// Platform-wide roles issued in access tokens by the User service.
/// Seller-team roles belong to the seller/product boundary and are not stored here.
/// </summary>
public static class PlatformRoleNames
{
    public const string PlatformAdmin = "PlatformAdmin";
}
