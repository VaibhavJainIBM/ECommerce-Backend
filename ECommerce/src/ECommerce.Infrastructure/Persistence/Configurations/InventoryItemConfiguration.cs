using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemConfiguration
    : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(
        EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable(
            "InventoryItems",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_InventoryItems_OnHand_NonNegative",
                    "[OnHandQuantity] >= 0");

                table.HasCheckConstraint(
                    "CK_InventoryItems_Reserved_NonNegative",
                    "[ReservedQuantity] >= 0");

                table.HasCheckConstraint(
                    "CK_InventoryItems_Reserved_NotGreaterThan_OnHand",
                    "[ReservedQuantity] <= [OnHandQuantity]");
            });

        builder.HasKey(inventory => inventory.Id);

        builder.Property(inventory => inventory.Id)
            .ValueGeneratedNever();

        builder.Property(inventory =>
                inventory.OnHandQuantity)
            .IsRequired();

        builder.Property(inventory =>
                inventory.ReservedQuantity)
            .IsRequired();

        builder.Property(inventory =>
                inventory.CreatedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(inventory =>
                inventory.UpdatedAtUtc)
            .HasPrecision(7);

        builder.Property(inventory =>
                inventory.RowVersion)
            .IsRowVersion()
            .IsRequired();

        // Available is calculated in C#:
        // OnHandQuantity - ReservedQuantity.
        builder.Ignore(inventory =>
            inventory.AvailableQuantity);

        // One inventory row for one listing
        // in one warehouse.
        builder.HasIndex(inventory => new
        {
            inventory.SellerId,
            inventory.WarehouseId,
            inventory.SellerListingId
        })
            .IsUnique();

        // Supports finding every warehouse
        // stocking a particular listing.
        builder.HasIndex(inventory => new
        {
            inventory.SellerId,
            inventory.SellerListingId
        });

        builder.HasOne(inventory =>
                inventory.Warehouse)
            .WithMany(warehouse =>
                warehouse.InventoryItems)
            .HasForeignKey(inventory => new
            {
                inventory.SellerId,
                inventory.WarehouseId
            })
            .HasPrincipalKey(warehouse => new
            {
                warehouse.SellerId,
                warehouse.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(inventory =>
                inventory.SellerListing)
            .WithMany(listing =>
                listing.InventoryItems)
            .HasForeignKey(inventory => new
            {
                inventory.SellerId,
                inventory.SellerListingId
            })
            .HasPrincipalKey(listing => new
            {
                listing.SellerId,
                listing.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}