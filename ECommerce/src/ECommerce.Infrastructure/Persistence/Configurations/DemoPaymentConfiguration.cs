using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class DemoPaymentConfiguration : IEntityTypeConfiguration<DemoPayment>
{
    public void Configure(EntityTypeBuilder<DemoPayment> builder)
    {
        builder.ToTable("DemoPayments", table =>
        {
            table.HasCheckConstraint("CK_DemoPayments_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_DemoPayments_Status", "[Status] IN ('Created', 'Succeeded', 'Failed', 'Cancelled')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsRequired();
        builder.HasIndex(x => new { x.OrderId, x.RequestKey }).IsUnique();
        builder.HasIndex(x => x.OrderId, "UX_DemoPayments_OneCreated").IsUnique().HasFilter("[Status] = 'Created'");
        builder.HasIndex(x => x.OrderId, "UX_DemoPayments_OneSucceeded").IsUnique().HasFilter("[Status] = 'Succeeded'");
        builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
