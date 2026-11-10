using ECommerce.User.Domain.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.User.Infrastructure.Identity;

public sealed class IdentitySeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<AdminSeedOptions> options,
    ILogger<IdentitySeeder> logger)
{
    private readonly AdminSeedOptions _options = options.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRoleExistsAsync(PlatformRoleNames.PlatformAdmin);
        if (!_options.Enabled)
        {
            logger.LogInformation("Admin seeding is disabled; platform role is ready.");
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var email = _options.Email.Trim().ToLowerInvariant();
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FirstName = _options.FirstName.Trim(),
                LastName = _options.LastName.Trim(),
                Email = email,
                UserName = email
            };

            var created = await userManager.CreateAsync(admin, _options.Password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Platform administrator could not be created: {FormatErrors(created)}");
            }
        }

        if (!admin.IsActive)
            throw new InvalidOperationException("The configured platform administrator is inactive.");

        if (!await userManager.IsInRoleAsync(admin, PlatformRoleNames.PlatformAdmin))
        {
            var assigned = await userManager.AddToRoleAsync(
                admin, PlatformRoleNames.PlatformAdmin);
            if (!assigned.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Platform administrator role could not be assigned: {FormatErrors(assigned)}");
            }
        }

        logger.LogInformation("Platform administrator identity is ready for {AdminEmail}.", email);
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName)) return;

        var result = await roleManager.CreateAsync(new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = roleName
        });

        if (!result.Succeeded && !await roleManager.RoleExistsAsync(roleName))
        {
            throw new InvalidOperationException(
                $"Platform role '{roleName}' could not be created: {FormatErrors(result)}");
        }
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join(" | ", result.Errors.Select(error =>
            $"{error.Code}: {error.Description}"));
}
