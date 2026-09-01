using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration
    : IEntityTypeConfiguration<Product>
{
    public void Configure(
        EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .ValueGeneratedNever();

        builder.Property(product => product.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(product => product.BrandName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(4000);

        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(product => product.CreatedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(product => product.UpdatedAtUtc)
            .HasPrecision(7);

        builder.HasIndex(product => product.Status);
    }
}