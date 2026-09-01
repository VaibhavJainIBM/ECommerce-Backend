using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class SellerRoleConfiguration
    : IEntityTypeConfiguration<SellerRole>
{
    public void Configure(
        EntityTypeBuilder<SellerRole> builder)
    {
        builder.ToTable("SellerRoles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new
        {
            x.SellerId,
            x.Id
        })
        .HasName("AK_SellerRoles_SellerId_Id");

        builder.HasIndex(x => new
        {
            x.SellerId,
            x.NormalizedName
        })
        .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsBuiltIn)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasPrecision(7);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.Roles)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}