using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace ECommerce.Infrastructure.Persistence.Repositories;

// Every warehouse/inventory query is filtered in SQL. A route policy alone cannot enforce warehouse assignments.
public sealed class SellerDataScope(ECommerceDbContext db, ICurrentUser currentUser)
{
    public IQueryable<Warehouse> Warehouses(Guid sellerId)
    {
        var userId = currentUser.UserId ?? Guid.Empty;
        return db.Warehouses.Where(w => w.SellerId == sellerId &&
            db.Users.Any(u => u.Id == userId && u.IsActive) &&
            db.SellerMembers.Any(m => m.SellerId == sellerId && m.UserId == userId && m.Status == SellerMemberStatus.Active &&
                m.RoleAssignments.Any(r => r.IsActive && r.SellerRole.IsActive &&
                    (r.SellerRole.NormalizedName == "OWNER" || r.SellerRole.NormalizedName == "MANAGER" ||
                     (r.SellerRole.NormalizedName == "WAREHOUSESTAFF" &&
                      m.WarehouseAssignments.Any(a => a.SellerId == sellerId && a.WarehouseId == w.Id && a.IsActive))))));
    }

    public IQueryable<InventoryItem> Inventory(Guid sellerId)
    {
        var warehouseIds = Warehouses(sellerId).Select(w => w.Id);
        return db.InventoryItems.Where(i => i.SellerId == sellerId && warehouseIds.Contains(i.WarehouseId));
    }
}
