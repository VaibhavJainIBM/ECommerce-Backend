using ECommerce.Application.Authentication;
using ECommerce.Application.Sellers;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Application.Catalog;
using ECommerce.Application.Listings;
using ECommerce.Application.Warehouses;
using ECommerce.Application.Inventory;

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

        services.AddScoped<
            IWarehouseService,
            WarehouseService>();
       
        services.AddScoped<
            ISellerLifecycleService,
            SellerLifecycleService>();

        services.AddScoped<
            IInventoryService,
            InventoryService>();

        return services;
    }
}