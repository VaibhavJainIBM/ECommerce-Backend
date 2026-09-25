using ECommerce.Order.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Order.Infrastructure.Persistence.Configurations;

public sealed class CustomerOrderConfiguration
    : IEntityTypeConfiguration<CustomerOrder>
{
    public void Configure(EntityTypeBuilder<CustomerOrder> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new
            {
                x.CustomerId,
                x.IdempotencyKey
            })
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.CustomerId,
            x.CreatedAtUtc
        });

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(3)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.ShippingFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ShippingLine1)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.ShippingLine2)
            .HasMaxLength(300);

        builder.Property(x => x.ShippingCity)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.ShippingState)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.ShippingPostalCode)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(x => x.ShippingCountryCode)
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(500);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
