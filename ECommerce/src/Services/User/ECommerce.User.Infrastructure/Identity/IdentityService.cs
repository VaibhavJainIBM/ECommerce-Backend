using ECommerce.User.Application.Abstractions.Identity;
using ECommerce.User.Application.Authentication;
using ECommerce.User.Application.Authentication.Models;
using ECommerce.User.Application.Common;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.User.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager)
    : IIdentityService
{
    public async Task<Result<UserAccount>> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = new ApplicationUser
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = normalizedEmail,
            UserName = normalizedEmail
        };

        var creationResult = await userManager.CreateAsync(user, password);
        cancellationToken.ThrowIfCancellationRequested();
        if (!creationResult.Succeeded)
        {
            if (creationResult.Errors.Any(error =>
                    error.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return Result<UserAccount>.Failure(AuthenticationErrors.DuplicateEmail);
            }

            return Result<UserAccount>.Failure(creationResult.Errors.Select(error =>
                AuthenticationErrors.IdentityValidation(error.Description)));
        }

        var roles = await userManager.GetRolesAsync(user);
        return Result<UserAccount>.Success(Map(user, roles.ToArray()));
    }

    public async Task<Result<UserAccount>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive || await userManager.IsLockedOutAsync(user))
        {
            return InvalidCredentials();
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            if (user.LockoutEnabled)
                await userManager.AccessFailedAsync(user);
            return InvalidCredentials();
        }

        if (user.AccessFailedCount > 0)
            await userManager.ResetAccessFailedCountAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        return Result<UserAccount>.Success(Map(user, roles.ToArray()));
    }

    private static Result<UserAccount> InvalidCredentials() =>
        Result<UserAccount>.Failure(AuthenticationErrors.InvalidCredentials);

    private static UserAccount Map(
        ApplicationUser user,
        IReadOnlyCollection<string> roles) =>
        new(user.Id, user.FirstName, user.LastName,
            user.Email ?? string.Empty, roles);
}
