using System.Net.Mail;
using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Abstractions.Identity;
using ECommerce.Application.Authentication.Dtos;
using ECommerce.Application.Authentication.Models;
using ECommerce.Application.Common;

namespace ECommerce.Application.Authentication;

public sealed class AuthenticationService(
    IIdentityService identityService,
    IAccessTokenGenerator accessTokenGenerator)
    : IAuthenticationService
{
    public async Task<Result<AuthResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationErrors = ValidateRegistration(request);

        if (validationErrors.Count > 0)
        {
            return Result<AuthResponseDto>.Failure(
                validationErrors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var accountResult = await identityService.CreateUserAsync(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.Email.Trim(),
            request.Password,
            cancellationToken);

        if (accountResult.IsFailure)
        {
            return Result<AuthResponseDto>.Failure(
                accountResult.Errors);
        }

        return CreateSuccessfulResponse(accountResult.Value!);
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationErrors = ValidateLogin(request);

        if (validationErrors.Count > 0)
        {
            return Result<AuthResponseDto>.Failure(
                validationErrors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var accountResult = await identityService.AuthenticateAsync(
            request.Email.Trim(),
            request.Password,
            cancellationToken);

        if (accountResult.IsFailure)
        {
            return Result<AuthResponseDto>.Failure(
                accountResult.Errors);
        }

        return CreateSuccessfulResponse(accountResult.Value!);
    }

    private Result<AuthResponseDto> CreateSuccessfulResponse(
        UserAccount account)
    {
        var token = accessTokenGenerator.Generate(account);

        var response = new AuthResponseDto(
            account.Id,
            account.FirstName,
            account.LastName,
            account.Email,
            token.Value,
            "Bearer",
            token.ExpiresAtUtc,
            account.PlatformRoles);

        return Result<AuthResponseDto>.Success(response);
    }

    private static List<Error> ValidateRegistration(
        RegisterRequestDto request)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            errors.Add(AuthenticationErrors.FirstNameRequired);
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            errors.Add(AuthenticationErrors.LastNameRequired);
        }

        if((request.FirstName?.Length ?? 0) > 100 ||
        (request.LastName?.Length ?? 0) > 100)
        {
            errors.Add(AuthenticationErrors.NameTooLong);
        }

        ValidateCredentials(
            request.Email,
            request.Password,
            errors);

        return errors;
    }

    private static List<Error> ValidateLogin(
        LoginRequestDto request)
    {
        var errors = new List<Error>();

        ValidateCredentials(
            request.Email,
            request.Password,
            errors);

        return errors;
    }

    private static void ValidateCredentials(
        string email,
        string password,
        ICollection<Error> errors)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(AuthenticationErrors.EmailRequired);
        }
        else if (!MailAddress.TryCreate(email, out _))
        {
            errors.Add(AuthenticationErrors.InvalidEmail);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add(AuthenticationErrors.PasswordRequired);
        }
    }
}