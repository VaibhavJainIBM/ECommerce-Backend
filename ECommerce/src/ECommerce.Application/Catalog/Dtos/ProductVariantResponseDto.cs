namespace ECommerce.Application.Catalog.Dtos;

public sealed record ProductVariantResponseDto(
    Guid VariantId,
    string Name,
    string VariantCode,
    string? Gtin,
    string Status,
    DateTimeOffset CreatedAtUtc);