using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] BETWEEN 1 AND 99");
            table.HasCheckConstraint("CK_OrderItems_UnitPrice", "[UnitPriceAmount] > 0");
            table.HasCheckConstraint("CK_OrderItems_LineTotal", "[LineTotal] = [UnitPriceAmount] * [Quantity]");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.OrderId, x.SellerListingId }).IsUnique();
        builder.HasIndex(x => new { x.SellerId, x.OrderId });
        builder.Property(x => x.SellerDisplayName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ProductTitle).HasMaxLength(250).IsRequired();
        builder.Property(x => x.VariantName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.SellerSku).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UnitPriceAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.LineTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.HasOne<SellerListing>().WithMany()
            .HasForeignKey(x => new { x.SellerId, x.SellerListingId })
            .HasPrincipalKey(x => new { x.SellerId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Allocations).WithOne()
            .HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
