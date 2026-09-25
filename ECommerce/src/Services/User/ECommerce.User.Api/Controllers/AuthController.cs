using System.IdentityModel.Tokens.Jwt;
using ECommerce.User.Application.Authentication;
using ECommerce.User.Application.Authentication.Dtos;
using ECommerce.User.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.User.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthenticationService authenticationService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthResponseDto>> RegisterAsync(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.RegisterAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value!)
            : ToProblem(result.Errors);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponseDto>> LoginAsync(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value!) : ToProblem(result.Errors);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponseDto), StatusCodes.Status200OK)]
    public ActionResult<CurrentUserResponseDto> GetCurrentUser()
    {
        var idValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(idValue, out var userId)) return Unauthorized();

        return Ok(new CurrentUserResponseDto(
            userId,
            User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty,
            User.FindFirst(JwtRegisteredClaimNames.GivenName)?.Value ?? string.Empty,
            User.FindFirst(JwtRegisteredClaimNames.FamilyName)?.Value ?? string.Empty,
            User.FindAll("role").Select(claim => claim.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));
    }

    private ActionResult ToProblem(IReadOnlyCollection<Error> errors)
    {
        if (errors.Any(error => error.Code == AuthenticationErrors.DuplicateEmail.Code))
            return ApiProblem(StatusCodes.Status409Conflict, "Registration conflict.",
                AuthenticationErrors.DuplicateEmail, errors);

        if (errors.Any(error => error.Code == AuthenticationErrors.InvalidCredentials.Code))
            return ApiProblem(StatusCodes.Status401Unauthorized, "Authentication failed.",
                AuthenticationErrors.InvalidCredentials, errors);

        var grouped = errors.GroupBy(error => error.Code).ToDictionary(
            group => group.Key,
            group => group.Select(error => error.Description).Distinct().ToArray());
        var details = new ValidationProblemDetails(grouped)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Detail = "Correct the supplied values and try again.",
            Instance = HttpContext.Request.Path
        };
        details.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return ValidationProblem(details);
    }

    private ObjectResult ApiProblem(
        int statusCode, string title, Error mainError, IReadOnlyCollection<Error> errors) =>
        Problem(statusCode: statusCode, title: title, detail: mainError.Description,
            instance: HttpContext.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = mainError.Code,
                ["errors"] = errors.Select(error => error.Description).ToArray()
            });
}
