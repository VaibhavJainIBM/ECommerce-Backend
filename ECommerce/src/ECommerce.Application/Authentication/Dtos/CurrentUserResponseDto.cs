namespace ECommerce.Application.Authentication.Dtos;

public sealed record CurrentUserResponseDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> PlatformRoles);