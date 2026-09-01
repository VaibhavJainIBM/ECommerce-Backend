using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class SellerMemberConfiguration
    : IEntityTypeConfiguration<SellerMember>
{
    public void Configure(
        EntityTypeBuilder<SellerMember> builder)
    {
        builder.ToTable("SellerMembers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasAlternateKey(x => new
        {
            x.SellerId,
            x.Id
        })
        .HasName("AK_SellerMembers_SellerId_Id");

        builder.HasIndex(x => new
        {
            x.SellerId,
            x.UserId
        })
        .IsUnique();

        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasPrecision(7);

        builder.Property(x => x.InvitedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(x => x.JoinedAtUtc)
            .HasPrecision(7);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}