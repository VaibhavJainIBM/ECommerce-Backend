using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Abstractions.Identity;
using ECommerce.Infrastructure.Authentication;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ECommerceDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 4;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ECommerceDbContext>();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IdentitySeeder>();

        services.AddOptions<JwtOptions>();

        services.AddSingleton<TimeProvider>(
            TimeProvider.System);

        services.AddSingleton<
            IAccessTokenGenerator,
            JwtTokenGenerator>();

        return services;
    }
}