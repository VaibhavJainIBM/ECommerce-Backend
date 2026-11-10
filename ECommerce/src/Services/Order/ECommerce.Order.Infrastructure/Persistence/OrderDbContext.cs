using ECommerce.Order.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Order.Infrastructure.Persistence;

public sealed class OrderDbContext(
    DbContextOptions<OrderDbContext> options)
    : DbContext(options)
{
    public DbSet<CustomerOrder> Orders => Set<CustomerOrder>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(OrderDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
