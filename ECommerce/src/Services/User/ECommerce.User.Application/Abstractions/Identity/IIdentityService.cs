using ECommerce.User.Application.Authentication.Models;
using ECommerce.User.Application.Common;

namespace ECommerce.User.Application.Abstractions.Identity;

public interface IIdentityService
{
    Task<Result<UserAccount>> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<Result<UserAccount>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
