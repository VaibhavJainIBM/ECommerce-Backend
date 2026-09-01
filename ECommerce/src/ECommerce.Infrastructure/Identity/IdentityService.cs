using ECommerce.Application.Abstractions.Identity;
using ECommerce.Application.Authentication;
using ECommerce.Application.Authentication.Models;
using ECommerce.Application.Common;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager)
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

        var creationResult = await userManager.CreateAsync(
            user,
            password);

        cancellationToken.ThrowIfCancellationRequested();

        if (!creationResult.Succeeded)
        {
            var duplicateEmail = creationResult.Errors.Any(error =>
                error.Code is "DuplicateEmail" or "DuplicateUserName");

            if (duplicateEmail)
            {
                return Result<UserAccount>.Failure(
                    AuthenticationErrors.DuplicateEmail);
            }

            var identityErrors = creationResult.Errors
                .Select(error =>
                    AuthenticationErrors.IdentityValidation(
                        error.Description))
                .ToArray();

            return Result<UserAccount>.Failure(identityErrors);
        }

        var roles = await userManager.GetRolesAsync(user);

        cancellationToken.ThrowIfCancellationRequested();

        return Result<UserAccount>.Success(
            MapUserAccount(user, roles.ToArray()));
    }

    public async Task<Result<UserAccount>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(
            email.Trim());

        cancellationToken.ThrowIfCancellationRequested();

        if (user is null)
        {
            return InvalidCredentials();
        }

        if (!user.IsActive)
        {
            return InvalidCredentials();
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return InvalidCredentials();
        }

        var passwordIsValid =
            await userManager.CheckPasswordAsync(user, password);

        cancellationToken.ThrowIfCancellationRequested();

        if (!passwordIsValid)
        {
            if (user.LockoutEnabled)
            {
                await userManager.AccessFailedAsync(user);
            }

            return InvalidCredentials();
        }

        if (user.AccessFailedCount > 0)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }

        var roles = await userManager.GetRolesAsync(user);

        cancellationToken.ThrowIfCancellationRequested();

        return Result<UserAccount>.Success(
            MapUserAccount(user, roles.ToArray()));
    }

    private static Result<UserAccount> InvalidCredentials()
    {
        return Result<UserAccount>.Failure(
            AuthenticationErrors.InvalidCredentials);
    }

    private static UserAccount MapUserAccount(
        ApplicationUser user,
        IReadOnlyCollection<string> platformRoles)
    {
        return new UserAccount(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            platformRoles);
    }
}