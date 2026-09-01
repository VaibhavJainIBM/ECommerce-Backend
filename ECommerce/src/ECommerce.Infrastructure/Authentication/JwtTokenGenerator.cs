using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Authentication.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Infrastructure.Authentication;

public sealed class JwtTokenGenerator(
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider)
    : IAccessTokenGenerator
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public AccessToken Generate(UserAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        ValidateOptions();

        var now = timeProvider.GetUtcNow();

        var expiresAtUtc = now.AddMinutes(
            _jwtOptions.AccessTokenMinutes);

        var claims = CreateClaims(account, now);

        var signingKey = new SymmetricSecurityKey(
            GetSigningKeyBytes());

        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        var tokenValue =
            new JwtSecurityTokenHandler().WriteToken(jwt);

        return new AccessToken(
            tokenValue,
            expiresAtUtc);
    }

    private static List<Claim> CreateClaims(
        UserAccount account,
        DateTimeOffset issuedAtUtc)
    {
        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                account.Id.ToString()),

            new(
                JwtRegisteredClaimNames.Email,
                account.Email),

            new(
                JwtRegisteredClaimNames.GivenName,
                account.FirstName),

            new(
                JwtRegisteredClaimNames.FamilyName,
                account.LastName),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                JwtRegisteredClaimNames.Iat,
                issuedAtUtc.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var distinctRoles = account.PlatformRoles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var role in distinctRoles)
        {
            claims.Add(new Claim("role", role));
        }

        return claims;
    }

    private byte[] GetSigningKeyBytes()
    {
        try
        {
            var keyBytes = Convert.FromBase64String(
                _jwtOptions.SigningKey);

            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException(
                    "JWT signing key must contain at least 32 bytes.");
            }

            return keyBytes;
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "JWT signing key must be a valid Base64 value.",
                exception);
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.Issuer))
        {
            throw new InvalidOperationException(
                "JWT issuer was not configured.");
        }

        if (string.IsNullOrWhiteSpace(_jwtOptions.Audience))
        {
            throw new InvalidOperationException(
                "JWT audience was not configured.");
        }

        if (string.IsNullOrWhiteSpace(_jwtOptions.SigningKey))
        {
            throw new InvalidOperationException(
                "JWT signing key was not configured.");
        }

        if (_jwtOptions.AccessTokenMinutes is < 1 or > 60)
        {
            throw new InvalidOperationException(
                "JWT access-token duration must be between 1 and 60 minutes.");
        }
    }
}