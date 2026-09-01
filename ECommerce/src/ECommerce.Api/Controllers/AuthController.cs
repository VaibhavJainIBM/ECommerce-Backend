using System.IdentityModel.Tokens.Jwt;
using ECommerce.Application.Authentication;
using ECommerce.Application.Authentication.Dtos;
using ECommerce.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthenticationService authenticationService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(
        typeof(AuthResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> RegisterAsync(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var result =
            await authenticationService.RegisterAsync(
                request,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            result.Value!);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(AuthResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> LoginAsync(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result =
            await authenticationService.LoginAsync(
                request,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return Ok(result.Value!);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(
        typeof(CurrentUserResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponseDto> GetCurrentUser()
    {
        var userIdClaim = User
            .FindFirst(JwtRegisteredClaimNames.Sub)?
            .Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var email = User
            .FindFirst(JwtRegisteredClaimNames.Email)?
            .Value ?? string.Empty;

        var firstName = User
            .FindFirst(JwtRegisteredClaimNames.GivenName)?
            .Value ?? string.Empty;

        var lastName = User
            .FindFirst(JwtRegisteredClaimNames.FamilyName)?
            .Value ?? string.Empty;

        var roles = User
            .FindAll("role")
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var response = new CurrentUserResponseDto(
            userId,
            email,
            firstName,
            lastName,
            roles);

        return Ok(response);
    }

    private ActionResult ToProblem(
        IReadOnlyCollection<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new InvalidOperationException(
                "A failed authentication result contained no errors.");
        }

        if (Contains(
                errors,
                AuthenticationErrors.DuplicateEmail))
        {
            return ApiProblem(
                StatusCodes.Status409Conflict,
                "Registration conflict.",
                AuthenticationErrors.DuplicateEmail.Description,
                AuthenticationErrors.DuplicateEmail.Code);
        }

        if (Contains(
                errors,
                AuthenticationErrors.InvalidCredentials))
        {
            return ApiProblem(
                StatusCodes.Status401Unauthorized,
                "Authentication failed.",
                AuthenticationErrors.InvalidCredentials.Description,
                AuthenticationErrors.InvalidCredentials.Code);
        }

        return ValidationProblemResponse(errors);
    }

    private ActionResult ValidationProblemResponse(
        IReadOnlyCollection<Error> errors)
    {
        var groupedErrors = errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.Description)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        var problemDetails =
            new ValidationProblemDetails(groupedErrors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title =
                    "One or more validation errors occurred.",
                Detail =
                    "Correct the supplied values and try again.",
                Instance = HttpContext.Request.Path
            };

        problemDetails.Extensions["traceId"] =
            HttpContext.TraceIdentifier;

        return ValidationProblem(problemDetails);
    }

    private ObjectResult ApiProblem(
        int statusCode,
        string title,
        string detail,
        string code)
    {
        return Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            instance: HttpContext.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
                ["traceId"] =
                    HttpContext.TraceIdentifier
            });
    }

    private static bool Contains(
        IEnumerable<Error> errors,
        Error expectedError)
    {
        return errors.Any(error =>
            string.Equals(
                error.Code,
                expectedError.Code,
                StringComparison.Ordinal));
    }
}