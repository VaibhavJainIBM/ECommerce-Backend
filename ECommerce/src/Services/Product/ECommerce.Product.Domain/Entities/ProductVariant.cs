using ECommerce.Product.Domain.Common;
using ECommerce.Product.Domain.Enums;

namespace ECommerce.Product.Domain.Entities;

public sealed class ProductVariant : AuditableEntity
{
    private ProductVariant()
    {
    }

    public ProductVariant(
        Guid productId,
        string name,
        string variantCode,
        string? gtin = null)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product ID is required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(variantCode);

        ProductId = productId;
        Name = name.Trim();

        VariantCode =
            variantCode
                .Trim()
                .ToUpperInvariant();

        Gtin = NormalizeGtin(gtin);

        Status = ProductVariantStatus.Draft;
    }

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public string Name { get; private set; }
        = string.Empty;

    public string VariantCode { get; private set; }
        = string.Empty;

    public string? Gtin { get; private set; }

    public ProductVariantStatus Status { get; private set; }

    public void Activate()
    {
        if (Status == ProductVariantStatus.Active)
            return;

        if (Status != ProductVariantStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only a draft variant can be activated.");
        }

        Status = ProductVariantStatus.Active;

        MarkUpdated();
    }

    public void Discontinue()
    {
        if (Status == ProductVariantStatus.Discontinued)
            return;

        Status = ProductVariantStatus.Discontinued;

        MarkUpdated();
    }

    private static string? NormalizeGtin(
        string? gtin)
    {
        if (string.IsNullOrWhiteSpace(gtin))
            return null;

        var normalized =
            gtin.Trim()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);

        if (normalized.Any(c => !char.IsDigit(c)) ||
            normalized.Length is not
                (8 or 12 or 13 or 14))
        {
            throw new ArgumentException(
                "GTIN must contain 8, 12, 13, or 14 digits.");
        }

        return normalized;
    }
}