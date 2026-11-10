using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ECommerce.User.Infrastructure.Persistence;

public sealed class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__UserConnection") ??
            "Server=localhost;Database=ECommerceUserDb;Trusted_Connection=True;" +
            "TrustServerCertificate=True;MultipleActiveResultSets=True";

        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new UserDbContext(options);
    }
}
