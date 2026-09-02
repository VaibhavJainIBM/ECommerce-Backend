using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

public sealed class SellerListing : AuditableEntity
{
    private SellerListing()
    {
    }

    public SellerListing(
        Guid sellerId,
        Guid productVariantId,
        string sellerSku,
        Money price)
    {
        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Seller ID is required.",
                nameof(sellerId));
        }

        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant ID is required.",
                nameof(productVariantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            sellerSku);

        var normalizedSellerSku =
            sellerSku.Trim();

        if (normalizedSellerSku.Length > 64)
        {
            throw new ArgumentException(
                "Seller SKU cannot exceed 64 characters.",
                nameof(sellerSku));
        }

        if (normalizedSellerSku.Any(character =>
                !IsAllowedSkuCharacter(character)))
        {
            throw new ArgumentException(
                "Seller SKU may contain only letters, " +
                "numbers, hyphens, underscores, and periods.",
                nameof(sellerSku));
        }

        ValidateListingPrice(price);

        SellerId = sellerId;
        ProductVariantId = productVariantId;

        SellerSku = normalizedSellerSku;

        NormalizedSellerSku =
            normalizedSellerSku.ToUpperInvariant();

        Price = price;

        Status = SellerListingStatus.Draft;
    }

    public Guid SellerId { get; private set; }

    public Seller Seller { get; private set; } = null!;

    public Guid ProductVariantId { get; private set; }

    public ProductVariant ProductVariant
    {
        get;
        private set;
    } = null!;

    public string SellerSku { get; private set; }
        = string.Empty;

    public string NormalizedSellerSku { get; private set; }
        = string.Empty;

    public Money Price { get; private set; } = null!;

    public SellerListingStatus Status { get; private set; }

    public byte[] RowVersion { get; private set; }
    = Array.Empty<byte>();

    public bool CanChangePrice =>
        Status is
            SellerListingStatus.Draft or
            SellerListingStatus.Rejected or
            SellerListingStatus.Paused or
            SellerListingStatus.Active;

    public ICollection<InventoryItem> InventoryItems
    {
        get;
        private set;
    } = new List<InventoryItem>();

    public void ChangePrice(Money price)
    {
        if (!CanChangePrice)
        {
            throw new InvalidOperationException(
                $"A listing with status '{Status}' " +
                "cannot change price.");
        }

        ValidateListingPrice(price);

        Price = price;

        MarkUpdated();
    }

    public void SubmitForReview()
    {
        if (Status != SellerListingStatus.Draft &&
            Status != SellerListingStatus.Rejected)
        {
            throw new InvalidOperationException(
                "Only a draft or rejected listing " +
                "can be submitted.");
        }

        Status = SellerListingStatus.PendingReview;

        MarkUpdated();
    }

    public void Approve()
    {
        if (Status !=
            SellerListingStatus.PendingReview)
        {
            throw new InvalidOperationException(
                "Only a pending listing can be approved.");
        }

        Status = SellerListingStatus.Active;

        MarkUpdated();
    }

    public void Reject()
    {
        if (Status !=
            SellerListingStatus.PendingReview)
        {
            throw new InvalidOperationException(
                "Only a pending listing can be rejected.");
        }

        Status = SellerListingStatus.Rejected;

        MarkUpdated();
    }

    public void Pause()
    {
        if (Status != SellerListingStatus.Active)
        {
            throw new InvalidOperationException(
                "Only an active listing can be paused.");
        }

        Status = SellerListingStatus.Paused;

        MarkUpdated();
    }

    public void Resume()
    {
        if (Status != SellerListingStatus.Paused)
        {
            throw new InvalidOperationException(
                "Only a paused listing can be resumed.");
        }

        Status = SellerListingStatus.Active;

        MarkUpdated();
    }

    public void Archive()
    {
        if (Status == SellerListingStatus.Archived)
        {
            return;
        }

        Status = SellerListingStatus.Archived;

        MarkUpdated();
    }

    private static void ValidateListingPrice(
        Money? price)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (price.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Listing price must be greater than zero.");
        }
    }

    private static bool IsAllowedSkuCharacter(
        char character)
    {
        return character is >= 'A' and <= 'Z' ||
               character is >= 'a' and <= 'z' ||
               character is >= '0' and <= '9' ||
               character is '-' or '_' or '.';
    }
}