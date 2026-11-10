using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Payment.Api.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(
                configuration.GetSection(
                    JwtOptions.SectionName))
            .Validate(
                x => !string.IsNullOrWhiteSpace(
                    x.Issuer))
            .Validate(
                x => !string.IsNullOrWhiteSpace(
                    x.Audience))
            .Validate(
                x => HasValidSigningKey(
                    x.SigningKey))
            .ValidateOnStart();

        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(
                (options, jwtAccessor) =>
                {
                    var jwt = jwtAccessor.Value;

                    options.MapInboundClaims = false;

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Convert.FromBase64String(
                                        jwt.SigningKey)),

                            ValidAlgorithms =
                            [
                                SecurityAlgorithms.HmacSha256
                            ],

                            ValidateIssuer = true,
                            ValidIssuer = jwt.Issuer,

                            ValidateAudience = true,
                            ValidAudience = jwt.Audience,

                            ValidateLifetime = true,

                            ClockSkew =
                                TimeSpan.FromSeconds(30)
                        };
                });

        services.AddAuthorization();

        return services;
    }

    private static bool HasValidSigningKey(
        string? signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
            return false;

        try
        {
            return Convert
                .FromBase64String(signingKey)
                .Length >= 32;
        }
        catch
        {
            return false;
        }
    }
}