using Microsoft.AspNetCore.Authorization;

namespace ECommerce.Api.Authorization;

public sealed class SellerAccessRequirement
    : IAuthorizationRequirement
{
    public SellerAccessRequirement(
        params string[] requiredRoles)
    {
        RequiredNormalizedRoles =
            (requiredRoles ?? Array.Empty<string>())
            .Where(role =>
                !string.IsNullOrWhiteSpace(role))
            .Select(role =>
                role.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);
    }

    public IReadOnlySet<string>
        RequiredNormalizedRoles
    { get; }
}