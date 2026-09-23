using ECommerce.Product.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Product.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration
    : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(
        EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.VariantCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Gtin)
            .HasMaxLength(14)
            .IsUnicode(false);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasPrecision(7);

        builder.HasIndex(x => new
        {
            x.ProductId,
            x.VariantCode
        })
            .IsUnique();

        builder.HasIndex(x => x.Gtin)
            .IsUnique()
            .HasFilter("[Gtin] IS NOT NULL");

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Variants)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}