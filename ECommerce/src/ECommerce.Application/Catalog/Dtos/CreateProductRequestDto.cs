namespace ECommerce.Application.Catalog.Dtos;

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