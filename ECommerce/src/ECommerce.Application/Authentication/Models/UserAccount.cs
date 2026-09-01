namespace ECommerce.Application.Authentication.Models;

public sealed record UserAccount(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyCollection<string> PlatformRoles);