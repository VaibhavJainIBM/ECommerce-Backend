using ECommerce.Order.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Order.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderApplication(
        this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
