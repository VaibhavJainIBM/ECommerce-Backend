using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class WarehouseConfiguration
    : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new
        {
            x.SellerId,
            x.Id
        })
        .HasName("AK_Warehouses_SellerId_Id");

        builder.HasIndex(x => new
        {
            x.SellerId,
            x.Code
        })
        .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasPrecision(7);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.Warehouses)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(x => x.Address, address =>
        {
            address.Property(x => x.Line1)
                .HasColumnName("AddressLine1")
                .HasMaxLength(200)
                .IsRequired();

            address.Property(x => x.Line2)
                .HasColumnName("AddressLine2")
                .HasMaxLength(200);

            address.Property(x => x.City)
                .HasColumnName("City")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(x => x.StateOrProvince)
                .HasColumnName("StateOrProvince")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(x => x.PostalCode)
                .HasColumnName("PostalCode")
                .HasMaxLength(20)
                .IsRequired();

            address.Property(x => x.CountryCode)
                .HasColumnName("CountryCode")
                .HasMaxLength(2)
                .IsFixedLength()
                .IsRequired();
        });

        builder.Navigation(x => x.Address)
            .IsRequired();
    }
}