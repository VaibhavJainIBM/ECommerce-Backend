using System.Net.Mail;
using ECommerce.User.Application.Abstractions.Authentication;
using ECommerce.User.Application.Abstractions.Identity;
using ECommerce.User.Application.Authentication.Dtos;
using ECommerce.User.Application.Authentication.Models;
using ECommerce.User.Application.Common;

namespace ECommerce.User.Application.Authentication;

public sealed class AuthenticationService(
    IIdentityService identityService,
    IAccessTokenGenerator accessTokenGenerator) : IAuthenticationService
{
    public async Task<Result<AuthResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = ValidateRegistration(request);
        if (errors.Count > 0)
        {
            return Result<AuthResponseDto>.Failure(errors);
        }

        var account = await identityService.CreateUserAsync(
            request.FirstName.Trim(), request.LastName.Trim(),
            request.Email.Trim(), request.Password, cancellationToken);

        return account.IsFailure
            ? Result<AuthResponseDto>.Failure(account.Errors)
            : CreateSuccessfulResponse(account.Value!);
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = ValidateCredentials(request.Email, request.Password);
        if (errors.Count > 0)
        {
            return Result<AuthResponseDto>.Failure(errors);
        }

        var account = await identityService.AuthenticateAsync(
            request.Email.Trim(), request.Password, cancellationToken);
        return account.IsFailure
            ? Result<AuthResponseDto>.Failure(account.Errors)
            : CreateSuccessfulResponse(account.Value!);
    }

    private Result<AuthResponseDto> CreateSuccessfulResponse(UserAccount account)
    {
        var token = accessTokenGenerator.Generate(account);
        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            account.Id, account.FirstName, account.LastName, account.Email,
            token.Value, "Bearer", token.ExpiresAtUtc, account.PlatformRoles));
    }

    private static List<Error> ValidateRegistration(RegisterRequestDto request)
    {
        var errors = ValidateCredentials(request.Email, request.Password);
        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors.Add(AuthenticationErrors.FirstNameRequired);
        if (string.IsNullOrWhiteSpace(request.LastName))
            errors.Add(AuthenticationErrors.LastNameRequired);
        if ((request.FirstName?.Length ?? 0) > 100 || (request.LastName?.Length ?? 0) > 100)
            errors.Add(AuthenticationErrors.NameTooLong);
        return errors;
    }

    private static List<Error> ValidateCredentials(string email, string password)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(email))
            errors.Add(AuthenticationErrors.EmailRequired);
        else if (!MailAddress.TryCreate(email, out _))
            errors.Add(AuthenticationErrors.InvalidEmail);
        if (string.IsNullOrWhiteSpace(password))
            errors.Add(AuthenticationErrors.PasswordRequired);
        return errors;
    }
}
