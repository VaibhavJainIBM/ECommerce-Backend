using ECommerce.Payment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext(
    DbContextOptions<PaymentDbContext> options)
    : DbContext(options)
{
    public DbSet<PaymentAttempt> PaymentAttempts =>
        Set<PaymentAttempt>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PaymentDbContext).Assembly);
    }
}