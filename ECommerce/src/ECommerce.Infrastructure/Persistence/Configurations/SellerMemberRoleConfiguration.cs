using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class SellerMemberRoleConfiguration
    : IEntityTypeConfiguration<SellerMemberRole>
{
    public void Configure(
        EntityTypeBuilder<SellerMemberRole> builder)
    {
        builder.ToTable("SellerMemberRoles");

        builder.HasKey(x => new
        {
            x.SellerId,
            x.SellerMemberId,
            x.SellerRoleId
        });

        builder.HasIndex(x => new
        {
            x.SellerId,
            x.SellerRoleId
        });

        builder.Property(x => x.AssignedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(x => x.RevokedAtUtc)
            .HasPrecision(7);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasOne(x => x.SellerMember)
            .WithMany(x => x.RoleAssignments)
            .HasForeignKey(x => new
            {
                x.SellerId,
                x.SellerMemberId
            })
            .HasPrincipalKey(x => new
            {
                x.SellerId,
                x.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SellerRole)
            .WithMany(x => x.MemberAssignments)
            .HasForeignKey(x => new
            {
                x.SellerId,
                x.SellerRoleId
            })
            .HasPrincipalKey(x => new
            {
                x.SellerId,
                x.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}