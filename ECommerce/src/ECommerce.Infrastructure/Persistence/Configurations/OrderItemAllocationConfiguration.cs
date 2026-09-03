using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class OrderItemAllocationConfiguration : IEntityTypeConfiguration<OrderItemAllocation>
{
    public void Configure(EntityTypeBuilder<OrderItemAllocation> builder)
    {
        builder.ToTable("OrderItemAllocations", table =>
            table.HasCheckConstraint("CK_OrderItemAllocations_Quantity", "[Quantity] BETWEEN 1 AND 99"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.OrderItemId, x.InventoryItemId }).IsUnique();
        builder.HasOne<InventoryItem>().WithMany()
            .HasForeignKey(x => x.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
