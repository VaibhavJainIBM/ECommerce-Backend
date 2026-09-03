using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace ECommerce.Api.Authorization;

public static class SellerAuthorizationExtensions
{
    public static IServiceCollection AddSellerAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(SellerPolicies.Management, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new SellerAccessRequirement(SellerRoleNames.Owner, SellerRoleNames.Manager));
            });
            options.AddPolicy(SellerPolicies.Inventory, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new SellerAccessRequirement(SellerRoleNames.Owner, SellerRoleNames.Manager, SellerRoleNames.WarehouseStaff));
            });
            options.AddPolicy(
                SellerPolicies.Access,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new SellerAccessRequirement());
                });

            options.AddPolicy(
                SellerPolicies.Owner,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new SellerAccessRequirement(
                            SellerRoleNames.Owner));
                });
        });

        services.AddScoped<
            IAuthorizationHandler,
            SellerAccessHandler>();

        services.AddSingleton<
            IAuthorizationMiddlewareResultHandler,
            SellerAuthorizationResultHandler>();

        return services;
    }
}
