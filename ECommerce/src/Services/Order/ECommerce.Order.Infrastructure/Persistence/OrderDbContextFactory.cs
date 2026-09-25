using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.Order.Infrastructure.Persistence;

public sealed class OrderDbContextFactory
    : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "ConnectionStrings__OrderConnection")
            ?? "Server=localhost;Database=ECommerceOrderDb;" +
               "Trusted_Connection=True;TrustServerCertificate=True;" +
               "MultipleActiveResultSets=True";

        var options =
            new DbContextOptionsBuilder<OrderDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        return new OrderDbContext(options);
    }
}
