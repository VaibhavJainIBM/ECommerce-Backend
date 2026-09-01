using ECommerce.Application.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Identity;

public sealed class IdentitySeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<AdminSeedOptions> options,
    ILogger<IdentitySeeder> logger)
{
    private readonly AdminSeedOptions _options =
        options.Value;

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureRoleExistsAsync(
            PlatformRoleNames.PlatformAdmin,
            cancellationToken);

        if (!_options.Enabled)
        {
            return;
        }

        var normalizedEmail =
            _options.Email.Trim().ToLowerInvariant();

        var adminUser =
            await userManager.FindByEmailAsync(
                normalizedEmail);

        cancellationToken.ThrowIfCancellationRequested();

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                FirstName = _options.FirstName.Trim(),
                LastName = _options.LastName.Trim(),
                Email = normalizedEmail,
                UserName = normalizedEmail
            };

            var creationResult =
                await userManager.CreateAsync(
                    adminUser,
                    _options.Password);

            cancellationToken.ThrowIfCancellationRequested();

            if (!creationResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "The platform administrator could not " +
                    $"be created: {FormatErrors(creationResult)}");
            }
        }

        if (!adminUser.IsActive)
        {
            throw new InvalidOperationException(
                "The configured platform administrator " +
                "exists but is inactive.");
        }

        var alreadyAdmin =
            await userManager.IsInRoleAsync(
                adminUser,
                PlatformRoleNames.PlatformAdmin);

        cancellationToken.ThrowIfCancellationRequested();

        if (!alreadyAdmin)
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    adminUser,
                    PlatformRoleNames.PlatformAdmin);

            cancellationToken.ThrowIfCancellationRequested();

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "The platform administrator role could " +
                    $"not be assigned: {FormatErrors(roleResult)}");
            }
        }

        logger.LogInformation(
            "Platform administrator identity is ready " +
            "for {AdminEmail}.",
            normalizedEmail);
    }

    private async Task EnsureRoleExistsAsync(
        string roleName,
        CancellationToken cancellationToken)
    {
        var roleExists =
            await roleManager.RoleExistsAsync(roleName);

        cancellationToken.ThrowIfCancellationRequested();

        if (roleExists)
        {
            return;
        }

        var role = new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = roleName
        };

        var roleResult =
            await roleManager.CreateAsync(role);

        cancellationToken.ThrowIfCancellationRequested();

        if (!roleResult.Succeeded)
        {
            var nowExists =
                await roleManager.RoleExistsAsync(roleName);

            if (!nowExists)
            {
                throw new InvalidOperationException(
                    $"Platform role '{roleName}' could not " +
                    $"be created: {FormatErrors(roleResult)}");
            }
        }
    }

    private static string FormatErrors(
        IdentityResult result)
    {
        return string.Join(
            " | ",
            result.Errors.Select(error =>
                $"{error.Code}: {error.Description}"));
    }
}