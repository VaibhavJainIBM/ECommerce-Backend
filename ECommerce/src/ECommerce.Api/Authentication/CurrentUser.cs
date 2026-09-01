using System.IdentityModel.Tokens.Jwt;
using ECommerce.Application.Abstractions.Authentication;

namespace ECommerce.Api.Authentication;

public sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var principal =
                httpContextAccessor.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var subject = principal
                .FindFirst(JwtRegisteredClaimNames.Sub)?
                .Value;

            return Guid.TryParse(
                subject,
                out var userId)
                ? userId
                : null;
        }
    }
}