using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;


namespace ECommerce.Domain.Entities;

public sealed class Seller : AuditableEntity
{
    private Seller()
    {
    }

    public Seller(
        string displayName,
        string legalBusinessName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalBusinessName);

        DisplayName = displayName.Trim();
        LegalBusinessName = legalBusinessName.Trim();
        Status = SellerStatus.PendingVerification;
    }

    public string DisplayName { get; private set; } = string.Empty;

    public string LegalBusinessName { get; private set; } = string.Empty;

    public SellerStatus Status { get; private set; }

    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    public ICollection<SellerMember> Members { get; private set; }
        = new List<SellerMember>();

    public ICollection<Warehouse> Warehouses { get; private set; }
        = new List<Warehouse>();

    public ICollection<SellerRole> Roles { get; private set; }
    = new List<SellerRole>();

    public void UpdateProfile(
        string displayName,
        string legalBusinessName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalBusinessName);

        DisplayName = displayName.Trim();
        LegalBusinessName = legalBusinessName.Trim();

        MarkUpdated();
    }

    public void SubmitForReview()
    {
        if (Status != SellerStatus.PendingVerification &&
            Status != SellerStatus.Rejected)
        {
            throw new InvalidOperationException(
                "Only a pending or rejected seller can submit for review.");
        }

        Status = SellerStatus.UnderReview;

        MarkUpdated();
    }

    public void Approve()
    {
        if (Status != SellerStatus.UnderReview)
        {
            throw new InvalidOperationException(
                "Only a seller under review can be approved.");
        }

        Status = SellerStatus.Active;
        ApprovedAtUtc = DateTimeOffset.UtcNow;

        MarkUpdated();
    }

    public void Suspend()
    {
        if (Status != SellerStatus.Active)
        {
            throw new InvalidOperationException(
                "Only an active seller can be suspended.");
        }

        Status = SellerStatus.Suspended;

        MarkUpdated();
    }

    public void Reactivate()
    {
        if (Status != SellerStatus.Suspended)
        {
            throw new InvalidOperationException(
                "Only a suspended seller can be reactivated.");
        }

        Status = SellerStatus.Active;

        MarkUpdated();
    }
}
