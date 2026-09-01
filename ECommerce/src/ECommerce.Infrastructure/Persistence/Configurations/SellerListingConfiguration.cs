using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class SellerListingConfiguration
    : IEntityTypeConfiguration<SellerListing>
{
    public void Configure(
        EntityTypeBuilder<SellerListing> builder)
    {
        builder.ToTable(
            "SellerListings",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_SellerListings_PriceAmount_Positive",
                    "[PriceAmount] > 0");
            });

        builder.HasKey(listing => listing.Id);

        builder.Property(listing => listing.Id)
            .ValueGeneratedNever();

        // Future inventory will reference a listing
        // through SellerId + ListingId.
        builder.HasAlternateKey(listing => new
        {
            listing.SellerId,
            listing.Id
        })
            .HasName(
                "AK_SellerListings_SellerId_Id");

        builder.Property(listing => listing.SellerSku)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(listing =>
                listing.NormalizedSellerSku)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(listing => listing.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(listing => listing.CreatedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(listing => listing.UpdatedAtUtc)
            .HasPrecision(7);

        builder.Property(listing => listing.RowVersion)
            .IsRowVersion()
            .IsRequired();

        // Money is stored inside SellerListings.
        // It does not receive a separate Money table.
        builder.OwnsOne(listing => listing.Price, price =>
        {
            price.Property(value => value.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(value => value.CurrencyCode)
                .HasColumnName("CurrencyCode")
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .IsRequired();
        });

        builder.Navigation(listing => listing.Price)
            .IsRequired();

        // The same seller cannot reuse a SKU,
        // even with different letter casing.
        builder.HasIndex(listing => new
        {
            listing.SellerId,
            listing.NormalizedSellerSku
        })
            .IsUnique();

        // MVP rule: one offer per seller per variant.
        builder.HasIndex(listing => new
        {
            listing.SellerId,
            listing.ProductVariantId
        })
            .IsUnique();

        // Used for a seller's listing dashboard.
        builder.HasIndex(listing => new
        {
            listing.SellerId,
            listing.Status
        });

        // Used for public offer comparison.
        builder.HasIndex(listing => new
        {
            listing.ProductVariantId,
            listing.Status
        });

        builder.HasOne(listing => listing.Seller)
            .WithMany(seller => seller.Listings)
            .HasForeignKey(listing => listing.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(listing =>
                listing.ProductVariant)
            .WithMany(variant => variant.Listings)
            .HasForeignKey(listing =>
                listing.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}