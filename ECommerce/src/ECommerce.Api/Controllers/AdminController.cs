using System.IdentityModel.Tokens.Jwt;
using ECommerce.Application.Administration.Dtos;
using ECommerce.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize(
    Roles = PlatformRoleNames.PlatformAdmin)]
[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(
        typeof(AdminProfileResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    public ActionResult<AdminProfileResponseDto>
        GetCurrentAdmin()
    {
        var userIdValue = User
            .FindFirst(JwtRegisteredClaimNames.Sub)?
            .Value;

        if (!Guid.TryParse(
                userIdValue,
                out var userId))
        {
            return Unauthorized();
        }

        var email = User
            .FindFirst(JwtRegisteredClaimNames.Email)?
            .Value ?? string.Empty;

        var platformRoles = User
            .FindAll("role")
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var response = new AdminProfileResponseDto(
            userId,
            email,
            platformRoles);

        return Ok(response);
    }
}