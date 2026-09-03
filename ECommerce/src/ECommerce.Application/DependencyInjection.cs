using ECommerce.Application.Authentication;
using ECommerce.Application.Sellers;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Application.Catalog;
using ECommerce.Application.Listings;
using ECommerce.Application.Warehouses;
using ECommerce.Application.Inventory;
using ECommerce.Application.Storefront;
using ECommerce.Application.Shopping;
using ECommerce.Application.Payments;
using ECommerce.Application.Fulfillment;
using ECommerce.Application.Catalog.Browsing;
using ECommerce.Application.SellerTeams;

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

        services.AddScoped<
            IStorefrontService,
            StorefrontService>();

        services.AddScoped<IShoppingService, ShoppingService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IFulfillmentService, FulfillmentService>();
        services.AddScoped<ICatalogBrowsingService, CatalogBrowsingService>();
        services.AddScoped<ISellerTeamService, SellerTeamService>();

        return services;
    }
}
