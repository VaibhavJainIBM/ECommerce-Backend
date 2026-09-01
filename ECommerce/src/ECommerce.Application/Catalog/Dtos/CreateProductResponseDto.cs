namespace ECommerce.Application.Catalog.Dtos;

public sealed record CreateProductResponseDto(
    Guid ProductId,
    string Title,
    string BrandName,
    string? Description,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<ProductVariantResponseDto> Variants);