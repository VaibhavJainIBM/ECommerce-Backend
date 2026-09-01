namespace ECommerce.Application.Catalog.Dtos;

public sealed class CreateProductVariantRequestDto
{
    public string? Name { get; init; }

    public string? VariantCode { get; init; }

    public string? Gtin { get; init; }
}