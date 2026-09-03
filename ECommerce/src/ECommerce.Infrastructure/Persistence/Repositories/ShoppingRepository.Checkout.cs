using ECommerce.Application.Common;
using ECommerce.Application.Shopping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed partial class ShoppingRepository
{
    public Task<Result<CheckoutResponseDto>> CheckoutAsync(
        CheckoutCommand command, CancellationToken cancellationToken = default)
    {
        return InTransactionAsync("cart:" + command.CustomerId, async () =>
        {
            if (!await CustomerIsActiveAsync(command.CustomerId, cancellationToken))
                return Result<CheckoutResponseDto>.Failure(ShoppingErrors.AccountUnavailable);

            // Look for a committed attempt BEFORE reading the now-empty cart.
            var previous = await dbContext.Orders.AsNoTracking().Include(x => x.Items)
                .SingleOrDefaultAsync(x => x.CustomerId == command.CustomerId &&
                    x.CheckoutKey == command.CheckoutKey, cancellationToken);
            if (previous is not null)
            {
                if (!string.Equals(previous.RequestHash, command.RequestHash, StringComparison.Ordinal))
                    return Result<CheckoutResponseDto>.Failure(ShoppingErrors.Conflict(
                        "This Idempotency-Key was already used with a different checkout request."));
                return Result<CheckoutResponseDto>.Success(new CheckoutResponseDto(MapOrder(previous), true));
            }

            var cart = await dbContext.Carts.Include(x => x.Items)
                .SingleOrDefaultAsync(x => x.CustomerId == command.CustomerId, cancellationToken);
            if (cart is null || cart.Items.Count == 0)
                return Result<CheckoutResponseDto>.Failure(ShoppingErrors.Conflict("The cart is empty."));
            if (!cart.RowVersion.AsSpan().SequenceEqual(command.CartRowVersion))
                return Result<CheckoutResponseDto>.Failure(ShoppingErrors.Conflict(
                    "The cart changed. GET /api/cart and submit its latest rowVersion and total."));

            var cartItems = cart.Items.OrderBy(x => x.SellerListingId).ToArray();
            var ids = cartItems.Select(x => x.SellerListingId).ToArray();
            var listings = await LoadListingsAsync(ids, cancellationToken);
            if (cartItems.Any(x => !listings.TryGetValue(x.SellerListingId, out var listing) || !IsPurchasable(listing)))
                return Result<CheckoutResponseDto>.Failure(ShoppingErrors.Conflict(
                    "One or more cart listings are no longer available. Refresh the cart."));
            if (listings.Values.Any(x => x.Price.CurrencyCode != command.CurrencyCode))
                return Result<CheckoutResponseDto>.Failure(ShoppingErrors.Conflict(
                    "The cart currency changed or contains mixed currencies. Refresh the cart."));

            // No client prices are accepted: the quote is compared with live database prices.
            var total = cartItems.Sum(x => listings[x.SellerListingId].Price.Amount * x.Quantity);
            if (total <= 0 || total > MaximumTotal)
                return Result<CheckoutResponseDto>.Failure(ShoppingErrors.Conflict("The order total exceeds the supported range."));
            if (total != command.ExpectedTotalAmount)
                return Result<CheckoutResponseDto>.Failure(ShoppingErrors.Conflict(
                    "Prices changed. Refresh the cart and confirm the new total before checking out."));

            var inventory = await dbContext.InventoryItems
                .Where(x => ids.Contains(x.SellerListingId) && x.Warehouse.Status == WarehouseStatus.Active)
                .OrderBy(x => x.SellerListingId).ThenBy(x => x.Id)
                .ToArrayAsync(cancellationToken);
            foreach (var item in cartItems)
            {
                var available = inventory.Where(x => x.SellerListingId == item.SellerListingId)
                    .Sum(x => (long)x.OnHandQuantity - x.ReservedQuantity);
                if (available < item.Quantity)
                    return Result<CheckoutResponseDto>.Failure(ShoppingErrors.Conflict(
                        $"Insufficient stock for listing '{item.SellerListingId}'. Refresh the cart."));
            }

            var order = new Order(command.CustomerId, command.CheckoutKey, command.RequestHash,
                command.RecipientName, command.Phone, command.ShippingAddress,
                DateTimeOffset.UtcNow.AddMinutes(30));
            foreach (var item in cartItems)
            {
                var listing = listings[item.SellerListingId];
                var line = new OrderItem(listing.SellerId, listing.Id, listing.ProductVariantId,
                    listing.Seller.DisplayName, listing.ProductVariant.Product.Title,
                    listing.ProductVariant.Name, listing.SellerSku, listing.Price.Amount,
                    listing.Price.CurrencyCode, item.Quantity);
                var remaining = item.Quantity;
                foreach (var stock in inventory.Where(x => x.SellerListingId == listing.Id))
                {
                    var quantity = Math.Min(remaining, stock.AvailableQuantity);
                    if (quantity <= 0) continue;
                    line.Allocate(stock, quantity);
                    stock.Reserve(quantity);
                    remaining -= quantity;
                    if (remaining == 0) break;
                }
                order.AddItem(line);
            }

            dbContext.Orders.Add(order);
            cart.Clear();
            // One SaveChanges + one transaction: order, allocations, stock and cart are all-or-nothing.
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<CheckoutResponseDto>.Success(new CheckoutResponseDto(MapOrder(order), false));
        }, cancellationToken);
    }
}
