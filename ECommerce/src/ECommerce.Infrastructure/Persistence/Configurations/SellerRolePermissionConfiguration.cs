using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class SellerRolePermissionConfiguration
    : IEntityTypeConfiguration<SellerRolePermission>
{
    public void Configure(
        EntityTypeBuilder<SellerRolePermission> builder)
    {
        builder.ToTable("SellerRolePermissions");

        builder.HasKey(x => new
        {
            x.SellerId,
            x.SellerRoleId,
            x.PermissionId
        });

        builder.HasIndex(x => x.PermissionId);

        builder.Property(x => x.GrantedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(x => x.RevokedAtUtc)
            .HasPrecision(7);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasOne(x => x.SellerRole)
            .WithMany(x => x.PermissionAssignments)
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

        builder.HasOne(x => x.Permission)
            .WithMany(x => x.RoleAssignments)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}