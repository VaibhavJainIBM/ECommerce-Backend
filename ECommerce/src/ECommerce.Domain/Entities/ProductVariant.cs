using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

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
                "Product ID is required.",
                nameof(productId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            variantCode);

        var normalizedName = name.Trim();

        var normalizedVariantCode =
            variantCode.Trim().ToUpperInvariant();

        if (normalizedName.Length > 150)
        {
            throw new ArgumentException(
                "Variant name cannot exceed 150 characters.",
                nameof(name));
        }

        if (normalizedVariantCode.Length > 64)
        {
            throw new ArgumentException(
                "Variant code cannot exceed 64 characters.",
                nameof(variantCode));
        }

        ProductId = productId;
        Name = normalizedName;
        VariantCode = normalizedVariantCode;
        Gtin = NormalizeGtin(gtin);
        Status = ProductVariantStatus.Draft;
    }

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public string Name { get; private set; }
        = string.Empty;

    // This is a shared platform catalog code,
    // not the seller's private SKU.
    public string VariantCode { get; private set; }
        = string.Empty;

    public string? Gtin { get; private set; }

    public ProductVariantStatus Status { get; private set; }

    public ICollection<SellerListing> Listings
    {
        get;
        private set;
    } = new List<SellerListing>();

    public void UpdateDetails(
        string name,
        string? gtin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.Trim();

        if (normalizedName.Length > 150)
        {
            throw new ArgumentException(
                "Variant name cannot exceed 150 characters.",
                nameof(name));
        }

        Name = normalizedName;
        Gtin = NormalizeGtin(gtin);

        MarkUpdated();
    }

    public void Activate()
    {
        if (Status == ProductVariantStatus.Active)
        {
            return;
        }

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
        if (Status ==
            ProductVariantStatus.Discontinued)
        {
            return;
        }

        Status = ProductVariantStatus.Discontinued;

        MarkUpdated();
    }

    private static string? NormalizeGtin(
        string? gtin)
    {
        if (string.IsNullOrWhiteSpace(gtin))
        {
            return null;
        }

        var normalizedGtin = gtin
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty);

        if (normalizedGtin.Any(character =>
                !char.IsDigit(character)) ||
            normalizedGtin.Length is not
                (8 or 12 or 13 or 14))
        {
            throw new ArgumentException(
                "GTIN must contain 8, 12, 13, " +
                "or 14 digits.",
                nameof(gtin));
        }

        return normalizedGtin;
    }
}