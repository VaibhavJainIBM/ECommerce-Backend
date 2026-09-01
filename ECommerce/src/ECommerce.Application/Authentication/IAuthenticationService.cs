using ECommerce.Application.Authentication.Dtos;
using ECommerce.Application.Common;

namespace ECommerce.Application.Authentication;

public interface IAuthenticationService
{
    Task<Result<AuthResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);
}