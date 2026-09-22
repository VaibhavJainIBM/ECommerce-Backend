using ECommerce.Payment.Application.Abstractions;
using ECommerce.Payment.Infrastructure.Orders;
using ECommerce.Payment.Infrastructure.Persistence;
using ECommerce.Payment.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string orderServiceBaseUrl)
    {
        services.AddDbContext<PaymentDbContext>(
            options =>
                options.UseSqlServer(
                    connectionString));

        services.AddScoped<
            IPaymentRepository,
            PaymentRepository>();

        services.AddHttpClient<
            IOrderPaymentClient,
            OrderPaymentClient>(
            client =>
            {
                client.BaseAddress =
                    new Uri(orderServiceBaseUrl);
            });

        return services;
    }
}