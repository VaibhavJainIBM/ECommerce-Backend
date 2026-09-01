using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class WarehouseAssignmentConfiguration
    : IEntityTypeConfiguration<WarehouseAssignment>
{
    public void Configure(
        EntityTypeBuilder<WarehouseAssignment> builder)
    {
        builder.ToTable("WarehouseAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasIndex(x => new
        {
            x.SellerId,
            x.SellerMemberId,
            x.WarehouseId
        })
        .IsUnique();

        builder.Property(x => x.AssignedAtUtc)
            .HasPrecision(7)
            .IsRequired();

        builder.Property(x => x.RemovedAtUtc)
            .HasPrecision(7);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasOne(x => x.SellerMember)
            .WithMany(x => x.WarehouseAssignments)
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

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.StaffAssignments)
            .HasForeignKey(x => new
            {
                x.SellerId,
                x.WarehouseId
            })
            .HasPrincipalKey(x => new
            {
                x.SellerId,
                x.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}