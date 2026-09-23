using ECommerce.Product.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Product.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProductApplication(
        this IServiceCollection services)
    {
        services.AddScoped<
            IProductService,
            ProductService>();

        return services;
    }
}