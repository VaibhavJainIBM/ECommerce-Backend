using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Product : AuditableEntity
{
    private Product()
    {
    }

    public Product(
        string title,
        string brandName,
        string? description = null)
    {
        SetDetails(
            title,
            brandName,
            description);

        Status = ProductStatus.Draft;
    }

    public string Title { get; private set; }
        = string.Empty;

    public string BrandName { get; private set; }
        = string.Empty;

    public string? Description { get; private set; }

    public ProductStatus Status { get; private set; }

    public ICollection<ProductVariant> Variants
    {
        get;
        private set;
    } = new List<ProductVariant>();

    public void UpdateDetails(
        string title,
        string brandName,
        string? description)
    {
        SetDetails(
            title,
            brandName,
            description);

        MarkUpdated();
    }

    public void Activate()
    {
        if (Status == ProductStatus.Active)
        {
            return;
        }

        if (Status != ProductStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only a draft product can be activated.");
        }

        Status = ProductStatus.Active;

        MarkUpdated();
    }

    public void Archive()
    {
        if (Status == ProductStatus.Archived)
        {
            return;
        }

        Status = ProductStatus.Archived;

        MarkUpdated();
    }

    private void SetDetails(
        string title,
        string brandName,
        string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            brandName);

        var normalizedTitle = title.Trim();

        var normalizedBrandName =
            brandName.Trim();

        var normalizedDescription =
            string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();

        if (normalizedTitle.Length > 250)
        {
            throw new ArgumentException(
                "Product title cannot exceed 250 characters.",
                nameof(title));
        }

        if (normalizedBrandName.Length > 150)
        {
            throw new ArgumentException(
                "Brand name cannot exceed 150 characters.",
                nameof(brandName));
        }

        if (normalizedDescription?.Length > 4000)
        {
            throw new ArgumentException(
                "Product description cannot exceed " +
                "4000 characters.",
                nameof(description));
        }

        Title = normalizedTitle;
        BrandName = normalizedBrandName;
        Description = normalizedDescription;
    }
}