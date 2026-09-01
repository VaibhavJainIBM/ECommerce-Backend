using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public sealed class ECommerceDbContext(
    DbContextOptions<ECommerceDbContext> options)
    : IdentityDbContext<
        ApplicationUser,
        IdentityRole<Guid>,
        Guid>(options)
{
    public DbSet<Seller> Sellers => Set<Seller>();

    public DbSet<SellerMember> SellerMembers => Set<SellerMember>();

    public DbSet<SellerRole> SellerRoles => Set<SellerRole>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<SellerMemberRole> SellerMemberRoles =>
        Set<SellerMemberRole>();

    public DbSet<SellerRolePermission> SellerRolePermissions =>
        Set<SellerRolePermission>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<WarehouseAssignment> WarehouseAssignments =>
        Set<WarehouseAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ECommerceDbContext).Assembly);
    }
}