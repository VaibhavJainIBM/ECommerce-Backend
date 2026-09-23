namespace ECommerce.Product.Application.Contracts;

public sealed record CreateProductRequest(
    string Title,
    string BrandName,
    string? Description);

public sealed record CreateVariantRequest(
    string Name,
    string VariantCode,
    string? Gtin);

public sealed record ProductVariantResponse(
    Guid Id,
    string Name,
    string VariantCode,
    string? Gtin,
    string Status);

public sealed record ProductResponse(
    Guid Id,
    string Title,
    string BrandName,
    string? Description,
    string Status,
    IReadOnlyCollection<ProductVariantResponse> Variants);