namespace ECommerce.Application.Administration.Dtos;

public sealed record AdminProfileResponseDto(
    Guid UserId,
    string Email,
    IReadOnlyCollection<string> PlatformRoles);