namespace ECommerce.Application.Authentication.Dtos;

public sealed record AuthResponseDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyCollection<string> PlatformRoles);