using ECommerce.Application.Authentication;
using ECommerce.Application.Sellers;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Application.Catalog;
using ECommerce.Application.Listings;

namespace ECommerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<
            IAuthenticationService,
            AuthenticationService>();

        services.AddScoped<
            ISellerOnboardingService,
            SellerOnboardingService>();

        services.AddScoped<
            ISellerQueryService,
            SellerQueryService>();

        services.AddScoped<
            IAdminCatalogService,
            AdminCatalogService>();

        services.AddScoped<
            ISellerListingService,
            SellerListingService>();

        return services;
    }
}