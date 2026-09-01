namespace ECommerce.Application.Authentication.Models;

public sealed record AccessToken(
    string Value,
    DateTimeOffset ExpiresAtUtc);