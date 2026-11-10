using ECommerce.Payment.Domain.Entities;
using ECommerce.Payment.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Payment.Infrastructure.Persistence.Configurations;

public sealed class PaymentAttemptConfiguration
    : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(
        EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("PaymentAttempts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.CurrencyCode)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsRequired();

        builder.HasIndex(
                x => new
                {
                    x.OrderId,
                    x.RequestKey
                })
            .IsUnique();

        builder.HasIndex(x => x.CustomerId);

        builder.HasIndex(
                x => x.OrderId,
                "UX_Payments_OneCreated")
            .IsUnique()
            .HasFilter("[Status] = 'Created'");

        builder.HasIndex(
                x => x.OrderId,
                "UX_Payments_OneSucceeded")
            .IsUnique()
            .HasFilter("[Status] = 'Succeeded'");
    }
}