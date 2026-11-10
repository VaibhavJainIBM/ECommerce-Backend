using ECommerce.User.Application.Authentication.Dtos;
using ECommerce.User.Application.Common;

namespace ECommerce.User.Application.Authentication;

public interface IAuthenticationService
{
    Task<Result<AuthResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);
}
