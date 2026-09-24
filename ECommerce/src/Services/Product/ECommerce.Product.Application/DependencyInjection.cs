using ECommerce.Product.Application.Catalog;
using ECommerce.Product.Application.Catalog.Browsing;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Product.Application.Catalog.Importing;

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
            IAdminCatalogQueryService,
            AdminCatalogQueryService>();

        services.AddScoped<
            ICatalogBrowsingService,
            CatalogBrowsingService>();

        services.AddScoped<
            IAdminCatalogImportService,
            AdminCatalogImportService>();   

        return services;
    }
}