namespace ECommerce.Application.Sellers.Dtos;

public sealed class CreateSellerRequestDto
{
    public string? DisplayName { get; init; }

    public string? LegalBusinessName { get; init; }
}