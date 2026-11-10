using ECommerce.Order.Application.Abstractions;
using ECommerce.Order.Infrastructure.Persistence;
using ECommerce.Order.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Order.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
