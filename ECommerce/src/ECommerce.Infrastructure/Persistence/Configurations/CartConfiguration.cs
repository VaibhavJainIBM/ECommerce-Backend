using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => x.CustomerId).IsUnique();
        builder.Property(x => x.RowVersion).IsRowVersion().IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasPrecision(7);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Items).WithOne()
            .HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
    }
}
