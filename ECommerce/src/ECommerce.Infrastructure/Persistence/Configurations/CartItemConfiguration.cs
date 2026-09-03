using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems", table =>
            table.HasCheckConstraint("CK_CartItems_Quantity", "[Quantity] BETWEEN 1 AND 99"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.CartId, x.SellerListingId }).IsUnique();
        builder.HasOne<SellerListing>().WithMany()
            .HasForeignKey(x => x.SellerListingId).OnDelete(DeleteBehavior.Restrict);
    }
}
