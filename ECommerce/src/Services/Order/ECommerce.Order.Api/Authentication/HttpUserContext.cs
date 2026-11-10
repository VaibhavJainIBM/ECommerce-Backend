using System.IdentityModel.Tokens.Jwt;
using ECommerce.Order.Application.Abstractions;

namespace ECommerce.Order.Api.Authentication;

public sealed class HttpUserContext(
    IHttpContextAccessor accessor)
    : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var user = accessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var subject = user
                .FindFirst(JwtRegisteredClaimNames.Sub)?
                .Value;

            return Guid.TryParse(subject, out var id)
                ? id
                : null;
        }
    }
}
