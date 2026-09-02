namespace ECommerce.Application.Warehouses.Dtos;

public sealed record WarehouseResponseDto(
    Guid WarehouseId,
    Guid SellerId,
    string Name,
    string Code,
    string Status,
    WarehouseAddressResponseDto Address,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record WarehouseAddressResponseDto(
    string Line1,
    string? Line2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string CountryCode);