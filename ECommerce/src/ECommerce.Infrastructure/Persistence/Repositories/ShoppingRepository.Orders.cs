using ECommerce.Application.Common;
using ECommerce.Application.Shopping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed partial class ShoppingRepository
{
    public async Task<Result<PagedOrdersResponseDto>> GetOrdersAsync(
        Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!await CustomerIsActiveAsync(customerId, cancellationToken))
            return Result<PagedOrdersResponseDto>.Failure(ShoppingErrors.AccountUnavailable);
        var query = dbContext.Orders.AsNoTracking().Where(x => x.CustomerId == customerId);
        var count = await query.CountAsync(cancellationToken);
        var orders = await query.Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return Result<PagedOrdersResponseDto>.Success(new PagedOrdersResponseDto(
            orders.Select(MapOrder).ToArray(), page, pageSize, count));
    }

    public async Task<Result<OrderResponseDto>> GetOrderAsync(
        Guid customerId, Guid orderId, CancellationToken cancellationToken = default)
    {
        if (!await CustomerIsActiveAsync(customerId, cancellationToken))
            return Result<OrderResponseDto>.Failure(ShoppingErrors.AccountUnavailable);
        var order = await dbContext.Orders.AsNoTracking().Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == orderId && x.CustomerId == customerId, cancellationToken);
        return order is null
            ? Result<OrderResponseDto>.Failure(ShoppingErrors.NotFound("The order was not found."))
            : Result<OrderResponseDto>.Success(MapOrder(order));
    }

    public async Task<Result<PagedSellerOrdersResponseDto>> GetSellerOrdersAsync(
        Guid sellerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders.AsNoTracking().Where(x => x.Items.Any(i => i.SellerId == sellerId));
        var count = await query.CountAsync(cancellationToken);
        var orders = await query.Include(x => x.Items.Where(i => i.SellerId == sellerId))
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        var items = orders.Select(order =>
        {
            var lines = order.Items.Where(x => x.SellerId == sellerId).Select(MapItem).ToArray();
            return new SellerOrderResponseDto(order.Id, order.OrderNumber, order.Status.ToString(),
                lines.Sum(x => x.LineTotal), order.CurrencyCode, order.RecipientName, order.Phone,
                MapAddress(order), lines, order.CreatedAtUtc, order.ExpiresAtUtc, order.PaymentMode);
        }).ToArray();
        return Result<PagedSellerOrdersResponseDto>.Success(new PagedSellerOrdersResponseDto(
            items, page, pageSize, count));
    }

    public Task<Result<OrderResponseDto>> CancelOrderAsync(
        Guid customerId, Guid orderId, CancellationToken cancellationToken = default)
    {
        return InTransactionAsync("order:" + orderId, async () =>
        {
            if (!await CustomerIsActiveAsync(customerId, cancellationToken))
                return Result<OrderResponseDto>.Failure(ShoppingErrors.AccountUnavailable);
            var order = await dbContext.Orders.Include(x => x.Items).ThenInclude(x => x.Allocations)
                .SingleOrDefaultAsync(x => x.Id == orderId && x.CustomerId == customerId, cancellationToken);
            if (order is null)
                return Result<OrderResponseDto>.Failure(ShoppingErrors.NotFound("The order was not found."));
            if (order.Status is OrderStatus.Paid or OrderStatus.PartiallyShipped or OrderStatus.Shipped)
                return Result<OrderResponseDto>.Failure(ShoppingErrors.Conflict("Paid or shipped orders cannot be cancelled by this MVP. Refunds are not implemented."));
            if (order.Status == OrderStatus.PendingPayment)
            {
                await ReleaseAllocationsAsync(order, cancellationToken);
                await CancelPendingPaymentsAsync(order.Id, cancellationToken);
                var now = DateTimeOffset.UtcNow;
                if (order.ExpiresAtUtc <= now) order.Expire(now);
                else order.Cancel();
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return Result<OrderResponseDto>.Success(MapOrder(order));
        }, cancellationToken);
    }

    public async Task<int> ExpireOrdersAsync(
        DateTimeOffset now, int batchSize, CancellationToken cancellationToken = default)
    {
        var ids = await dbContext.Orders.AsNoTracking()
            .Where(x => x.Status == OrderStatus.PendingPayment && x.ExpiresAtUtc <= now)
            .OrderBy(x => x.ExpiresAtUtc).ThenBy(x => x.Id)
            .Take(Math.Clamp(batchSize, 1, 100)).Select(x => x.Id).ToArrayAsync(cancellationToken);
        var expired = 0;
        foreach (var id in ids)
        {
            var result = await InTransactionAsync("order:" + id, async () =>
            {
                var order = await dbContext.Orders.Include(x => x.Items).ThenInclude(x => x.Allocations)
                    .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (order is null || order.Status != OrderStatus.PendingPayment || order.ExpiresAtUtc > now)
                    return Result<bool>.Success(false);
                await ReleaseAllocationsAsync(order, cancellationToken);
                await CancelPendingPaymentsAsync(order.Id, cancellationToken);
                order.Expire(now);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result<bool>.Success(true);
            }, cancellationToken);
            if (result.IsSuccess && result.Value) expired++;
            // A fresh snapshot on each pass avoids stale tracked inventory across orders.
            dbContext.ChangeTracker.Clear();
        }
        return expired;
    }

    private async Task ReleaseAllocationsAsync(Order order, CancellationToken cancellationToken)
    {
        var allocations = order.Items.SelectMany(x => x.Allocations)
            .GroupBy(x => x.InventoryItemId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var ids = allocations.Keys.ToArray();
        var inventory = await dbContext.InventoryItems.Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.Id).ToArrayAsync(cancellationToken);
        if (inventory.Length != ids.Length)
            throw new InvalidOperationException("Reserved order inventory is missing.");
        foreach (var item in inventory) item.Release(allocations[item.Id]);
    }

    private static ShippingAddressDto MapAddress(Order order) => new(
        order.ShippingAddress.Line1, order.ShippingAddress.City, order.ShippingAddress.StateOrProvince,
        order.ShippingAddress.PostalCode, order.ShippingAddress.CountryCode, order.ShippingAddress.Line2);

    private static OrderItemResponseDto MapItem(OrderItem item) => new(
        item.Id, item.SellerId, item.SellerDisplayName, item.SellerListingId, item.ProductVariantId,
        item.ProductTitle, item.VariantName, item.SellerSku, item.UnitPriceAmount, item.CurrencyCode,
        item.Quantity, item.LineTotal, item.ShippedAtUtc);

    private static OrderResponseDto MapOrder(Order order) => new(
        order.Id, order.OrderNumber, order.Status.ToString(), order.TotalAmount, order.CurrencyCode,
        order.RecipientName, order.Phone, MapAddress(order),
        order.Items.OrderBy(x => x.SellerId).ThenBy(x => x.SellerListingId).Select(MapItem).ToArray(),
        order.CreatedAtUtc, order.ExpiresAtUtc, Convert.ToBase64String(order.RowVersion),
        order.PaidAtUtc, order.PaymentMode);
}
