using ECommerce.Product.Application.Catalog;
using ECommerce.Product.Application.Catalog.Browsing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Product.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProductApplication(
        this IServiceCollection services)
    {
        services.AddScoped<
            IAdminCatalogService,
            AdminCatalogService>();

        services.AddScoped<
            ICatalogBrowsingService,
            CatalogBrowsingService>();

        return services;
    }
}