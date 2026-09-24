using System.IdentityModel.Tokens.Jwt;
using ECommerce.Product.Application.Abstractions;
    
namespace ECommerce.Product.Api.Authentication;

public sealed class HttpUserContext(
    IHttpContextAccessor accessor)
    : ICurrentUser,
      IAccessTokenAccessor
{
    public Guid? UserId
    {
        get
        {
            var user =
                accessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var subject =
                user.FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                    .Value;

            return Guid.TryParse(
                subject,
                out var id)
                ? id
                : null;
        }
    }

    public string? AccessToken
    {
        get
        {
            var authorization =
                accessor.HttpContext?
                    .Request
                    .Headers
                    .Authorization
                    .ToString();

            const string prefix = "Bearer ";

            if (string.IsNullOrWhiteSpace(
                    authorization))
            {
                return null;
            }

            if (!authorization.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return authorization[
                    prefix.Length..]
                .Trim();
        }
    }
}