using ECommerce.Application.Common;
using ECommerce.Application.Shopping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed partial class ShoppingRepository
{
    public Task<Result<SellerOrderResponseDto>> ShipSellerOrderAsync(
        Guid actorId, Guid sellerId, Guid orderId, CancellationToken cancellationToken = default)
    {
        // Payment, cancellation, expiry and shipment use the same order lock.
        return InTransactionAsync("order:" + orderId, async () =>
        {
            if (!await CustomerIsActiveAsync(actorId, cancellationToken))
                return Result<SellerOrderResponseDto>.Failure(ShoppingErrors.AccountUnavailable);
            if (!await CanManageFulfillmentAsync(actorId, sellerId, cancellationToken))
                return Result<SellerOrderResponseDto>.Failure(ShoppingErrors.NotFound("The seller order was not found."));

            var order = await dbContext.Orders.Include(x => x.Items).ThenInclude(x => x.Allocations)
                .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
            var sellerItems = order?.Items.Where(x => x.SellerId == sellerId).ToArray() ?? [];
            if (order is null || sellerItems.Length == 0)
                return Result<SellerOrderResponseDto>.Failure(ShoppingErrors.NotFound("The seller order was not found."));
            if (order.Status is not (OrderStatus.Paid or OrderStatus.PartiallyShipped or OrderStatus.Shipped))
                return Result<SellerOrderResponseDto>.Failure(ShoppingErrors.Conflict(
                    "Only a paid order can be shipped. Pending, cancelled and expired orders cannot be shipped."));

            var toShip = sellerItems.Where(x => !x.ShippedAtUtc.HasValue).ToArray();
            // A retry never consumes stock twice, including after other sellers ship.
            if (toShip.Length == 0)
                return Result<SellerOrderResponseDto>.Success(MapSellerFulfillment(order, sellerId));
            if (order.Status == OrderStatus.Shipped)
                return Result<SellerOrderResponseDto>.Failure(ShoppingErrors.Conflict("The order shipment state is inconsistent."));
            if (toShip.Any(x => x.Allocations.Sum(a => (long)a.Quantity) != x.Quantity))
                return Result<SellerOrderResponseDto>.Failure(ShoppingErrors.Conflict("The order's inventory allocation is incomplete."));

            var allocations = toShip.SelectMany(x => x.Allocations)
                .GroupBy(x => x.InventoryItemId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
            var inventoryIds = allocations.Keys.ToArray();
            var inventory = await dbContext.InventoryItems.Where(x => inventoryIds.Contains(x.Id))
                .OrderBy(x => x.Id).ToArrayAsync(cancellationToken);
            if (inventory.Length != inventoryIds.Length || inventory.Any(x =>
                    x.SellerId != sellerId || x.ReservedQuantity < allocations[x.Id] ||
                    x.OnHandQuantity < allocations[x.Id]))
                return Result<SellerOrderResponseDto>.Failure(ShoppingErrors.Conflict("The order's reserved stock is unavailable."));

            var byId = inventory.ToDictionary(x => x.Id);
            if (toShip.Any(line => line.Allocations.Any(allocation =>
                    byId[allocation.InventoryItemId].SellerListingId != line.SellerListingId)))
                return Result<SellerOrderResponseDto>.Failure(ShoppingErrors.Conflict("The order's inventory allocation does not match its listing."));

            foreach (var item in inventory) item.Ship(allocations[item.Id]);
            var now = DateTimeOffset.UtcNow;
            foreach (var line in toShip) line.MarkShipped(now);
            order.RefreshShipmentStatus();
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<SellerOrderResponseDto>.Success(MapSellerFulfillment(order, sellerId));
        }, cancellationToken);
    }

    private async Task<bool> CanManageFulfillmentAsync(
        Guid actorId, Guid sellerId, CancellationToken cancellationToken)
    {
        var memberId = await dbContext.SellerMembers.AsNoTracking()
            .Where(x => x.UserId == actorId && x.SellerId == sellerId && x.Status == SellerMemberStatus.Active)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
        if (!memberId.HasValue) return false;

        return await dbContext.SellerMemberRoles.AsNoTracking().AnyAsync(x =>
            x.SellerId == sellerId && x.SellerMemberId == memberId.Value && x.IsActive &&
            x.SellerRole.IsActive && x.SellerRole.SellerId == sellerId &&
            (x.SellerRole.NormalizedName == "OWNER" || x.SellerRole.NormalizedName == "MANAGER"), cancellationToken);
    }

    private static SellerOrderResponseDto MapSellerFulfillment(Order order, Guid sellerId)
    {
        var lines = order.Items.Where(x => x.SellerId == sellerId)
            .OrderBy(x => x.SellerListingId).Select(MapItem).ToArray();
        return new SellerOrderResponseDto(order.Id, order.OrderNumber, order.Status.ToString(),
            lines.Sum(x => x.LineTotal), order.CurrencyCode, order.RecipientName, order.Phone,
            MapAddress(order), lines, order.CreatedAtUtc, order.ExpiresAtUtc, order.PaymentMode);
    }
}
