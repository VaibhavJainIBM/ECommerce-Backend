using ECommerce.User.Application.Abstractions.Authentication;
using ECommerce.User.Application.Abstractions.Identity;
using ECommerce.User.Infrastructure.Authentication;
using ECommerce.User.Infrastructure.Identity;
using ECommerce.User.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.User.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddUserInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<UserDbContext>(options => options.UseSqlServer(connectionString));
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
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<UserDbContext>();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IdentitySeeder>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAccessTokenGenerator, JwtTokenGenerator>();
        return services;
    }
}
