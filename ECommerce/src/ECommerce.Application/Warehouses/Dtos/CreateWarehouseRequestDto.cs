namespace ECommerce.Application.Warehouses.Dtos;

public sealed class CreateWarehouseRequestDto
{
    public string? Name { get; init; }

    public string? Code { get; init; }

    public CreateWarehouseAddressRequestDto? Address { get; init; }
}

public sealed class CreateWarehouseAddressRequestDto
{
    public string? Line1 { get; init; }

    public string? Line2 { get; init; }

    public string? City { get; init; }

    public string? StateOrProvince { get; init; }

    public string? PostalCode { get; init; }

    public string? CountryCode { get; init; }
}