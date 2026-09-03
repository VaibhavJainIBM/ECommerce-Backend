using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_TotalAmount", "[TotalAmount] > 0");
            table.HasCheckConstraint("CK_Orders_Status", "[Status] IN ('PendingPayment', 'Cancelled', 'Expired', 'Paid', 'PartiallyShipped', 'Shipped')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.CustomerId, x.CheckoutKey }).IsUnique();
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.Status, x.ExpiresAtUtc });
        builder.Property(x => x.OrderNumber).HasMaxLength(36).IsUnicode(false).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(x => x.RecipientName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasPrecision(7).IsRequired();
        builder.Property(x => x.PaidAtUtc).HasPrecision(7);
        builder.Property(x => x.PaymentMode).HasMaxLength(16).IsUnicode(false);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasPrecision(7);
        builder.Property(x => x.RowVersion).IsRowVersion().IsRequired();
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Items).WithOne()
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(x => x.ShippingAddress, address =>
        {
            address.Property(x => x.Line1).HasColumnName("ShippingLine1").HasMaxLength(200).IsRequired();
            address.Property(x => x.Line2).HasColumnName("ShippingLine2").HasMaxLength(200);
            address.Property(x => x.City).HasColumnName("ShippingCity").HasMaxLength(100).IsRequired();
            address.Property(x => x.StateOrProvince).HasColumnName("ShippingStateOrProvince").HasMaxLength(100).IsRequired();
            address.Property(x => x.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(20).IsRequired();
            address.Property(x => x.CountryCode).HasColumnName("ShippingCountryCode").HasMaxLength(2).IsFixedLength().IsRequired();
        });
        builder.Navigation(x => x.ShippingAddress).IsRequired();
    }
}
