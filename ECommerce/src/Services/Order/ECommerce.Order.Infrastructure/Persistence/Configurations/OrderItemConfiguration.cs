using ECommerce.Order.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Order.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration
    : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.SellerId);

        builder.Property(x => x.SellerSku)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ProductTitle)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.VariantName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.LineTotal)
            .HasPrecision(18, 2);
    }
}
