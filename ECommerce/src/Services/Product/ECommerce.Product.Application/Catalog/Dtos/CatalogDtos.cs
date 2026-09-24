namespace ECommerce.Product.Application.Catalog.Dtos;

public sealed class CreateProductRequestDto
{
    public string? Title { get; init; }

    public string? BrandName { get; init; }

    public string? Description { get; init; }

    public List<CreateProductVariantRequestDto?>? Variants
    {
        get;
        init;
    }
}

public sealed class CreateProductVariantRequestDto
{
    public string? Name { get; init; }

    public string? VariantCode { get; init; }

    public string? Gtin { get; init; }
}

public sealed record ProductVariantResponseDto(
    Guid VariantId,
    string Name,
    string VariantCode,
    string? Gtin,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateProductResponseDto(
    Guid ProductId,
    string Title,
    string BrandName,
    string? Description,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<ProductVariantResponseDto> Variants);