using ECommerce.User.Application.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.User.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddUserApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        return services;
    }
}
