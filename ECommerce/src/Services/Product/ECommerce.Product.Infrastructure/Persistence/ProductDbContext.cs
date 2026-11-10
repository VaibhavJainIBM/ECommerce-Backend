using ECommerce.Product.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ProductEntity = ECommerce.Product.Domain.Entities.Product;

namespace ECommerce.Product.Infrastructure.Persistence;

public sealed class ProductDbContext(
    DbContextOptions<ProductDbContext> options)
    : DbContext(options)
{
    public DbSet<ProductEntity> Products =>
        Set<ProductEntity>();

    public DbSet<ProductVariant> ProductVariants =>
        Set<ProductVariant>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ProductDbContext).Assembly);
    }
}