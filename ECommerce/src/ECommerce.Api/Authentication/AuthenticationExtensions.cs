using ECommerce.Application.Abstractions.Authentication;
using System.IdentityModel.Tokens.Jwt;
using ECommerce.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Api.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Issuer),
                "Jwt:Issuer is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Audience),
                "Jwt:Audience is required.")
            .Validate(
                options => HasValidSigningKey(options.SigningKey),
                "Jwt:SigningKey must be valid Base64 containing at least 32 bytes.")
            .Validate(
                options =>
                    options.AccessTokenMinutes >= 1 &&
                    options.AccessTokenMinutes <= 60,
                "Jwt:AccessTokenMinutes must be between 1 and 60.")
            .ValidateOnStart();

        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(
                (bearerOptions, jwtOptionsAccessor) =>
                {
                    var jwtOptions = jwtOptionsAccessor.Value;

                    var signingKeyBytes =
                        Convert.FromBase64String(
                            jwtOptions.SigningKey);

                    bearerOptions.MapInboundClaims = false;

                    bearerOptions.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    signingKeyBytes),

                            ValidAlgorithms = new[]
                            {
                                SecurityAlgorithms.HmacSha256
                            },

                            ValidateIssuer = true,
                            ValidIssuer = jwtOptions.Issuer,

                            ValidateAudience = true,
                            ValidAudience = jwtOptions.Audience,

                            ValidateLifetime = true,
                            RequireExpirationTime = true,
                            RequireSignedTokens = true,

                            ClockSkew =
                                TimeSpan.FromSeconds(30),

                            NameClaimType =
                                JwtRegisteredClaimNames.Sub,

                            RoleClaimType = "role"
                        };
                });

        services.AddAuthorization();

        services.AddHttpContextAccessor();

        services.AddScoped<
            ICurrentUser,
            CurrentUser>();

        return services;
    }

    private static bool HasValidSigningKey(
        string? signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            return false;
        }

        try
        {
            return Convert
                .FromBase64String(signingKey)
                .Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}