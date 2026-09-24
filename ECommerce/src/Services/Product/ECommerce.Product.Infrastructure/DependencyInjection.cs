using ECommerce.Product.Application.Abstractions;
using ECommerce.Product.Infrastructure.Persistence;
using ECommerce.Product.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Product.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProductInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ProductDbContext>(
            options =>
                options.UseSqlServer(
                    connectionString));

        services.AddScoped<
            IProductCatalogRepository,
            ProductCatalogRepository>();

        services.AddScoped<
            ICatalogBrowsingRepository,
            CatalogBrowsingRepository>();

        return services;
    }
}