using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration
    : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(
        EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(variant => variant.Id);

        builder.Property(variant => variant.Id)
            .ValueGeneratedNever();

        builder.Property(variant => variant.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(variant => variant.VariantCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(variant => variant.Gtin)
            .HasMaxLength(14)
            .IsUnicode(false);

        builder.Property(variant => variant.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(variant => variant.CreatedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(variant => variant.UpdatedAtUtc)
            .HasPrecision(7);

        // A variant code is unique inside its product.
        builder.HasIndex(variant => new
        {
            variant.ProductId,
            variant.VariantCode
        })
            .IsUnique();

        // A GTIN is globally unique when supplied.
        builder.HasIndex(variant => variant.Gtin)
            .IsUnique()
            .HasFilter("[Gtin] IS NOT NULL");

        builder.HasIndex(variant => new
        {
            variant.ProductId,
            variant.Status
        });

        builder.HasOne(variant => variant.Product)
            .WithMany(product => product.Variants)
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}