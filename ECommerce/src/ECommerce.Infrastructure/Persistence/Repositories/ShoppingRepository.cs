using System.Data;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Application.Shopping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed partial class ShoppingRepository(ECommerceDbContext dbContext)
    : IShoppingRepository
{
    private const decimal MaximumTotal = 9_999_999_999_999_999.99m;

    public async Task<Result<CartResponseDto>> GetCartAsync(
        Guid customerId, CancellationToken cancellationToken = default)
    {
        if (!await CustomerIsActiveAsync(customerId, cancellationToken))
            return Result<CartResponseDto>.Failure(ShoppingErrors.AccountUnavailable);

        var cart = await dbContext.Carts.AsNoTracking().Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
        return Result<CartResponseDto>.Success(await MapCartAsync(cart, cancellationToken));
    }

    public Task<Result<CartResponseDto>> SetCartItemAsync(
        Guid customerId, Guid listingId, int quantity, CancellationToken cancellationToken = default)
    {
        return InTransactionAsync("cart:" + customerId, async () =>
        {
            if (!await CustomerIsActiveAsync(customerId, cancellationToken))
                return Result<CartResponseDto>.Failure(ShoppingErrors.AccountUnavailable);
            if (listingId == Guid.Empty || quantity is < 1 or > 99)
                return Result<CartResponseDto>.Failure(ShoppingErrors.Validation("A listing ID and quantity between 1 and 99 are required."));

            var listings = await LoadListingsAsync([listingId], cancellationToken);
            if (!listings.TryGetValue(listingId, out var listing))
                return Result<CartResponseDto>.Failure(ShoppingErrors.NotFound("The listing was not found."));
            if (!IsPurchasable(listing))
                return Result<CartResponseDto>.Failure(ShoppingErrors.Conflict("The listing is not available for purchase."));

            var stock = await LoadAvailabilityAsync([listingId], cancellationToken);
            if (stock.GetValueOrDefault(listingId) < quantity)
                return Result<CartResponseDto>.Failure(ShoppingErrors.Conflict("There is not enough available stock for this quantity."));

            var cart = await dbContext.Carts.Include(x => x.Items)
                .SingleOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
            if (cart is null)
            {
                cart = new Cart(customerId);
                dbContext.Carts.Add(cart);
            }

            var otherIds = cart.Items.Where(x => x.SellerListingId != listingId)
                .Select(x => x.SellerListingId).ToArray();
            if (otherIds.Length >= 50 && !cart.Items.Any(x => x.SellerListingId == listingId))
                return Result<CartResponseDto>.Failure(ShoppingErrors.Conflict("A cart can contain at most 50 distinct listings."));
            if (otherIds.Length > 0 && await dbContext.SellerListings.AnyAsync(
                    x => otherIds.Contains(x.Id) && x.Price.CurrencyCode != listing.Price.CurrencyCode,
                    cancellationToken))
                return Result<CartResponseDto>.Failure(ShoppingErrors.Conflict("All cart items must use the same currency. Remove items in the other currency first."));

            cart.SetItem(listingId, quantity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<CartResponseDto>.Success(await MapCartAsync(cart, cancellationToken));
        }, cancellationToken);
    }

    public Task<Result<CartResponseDto>> RemoveCartItemAsync(
        Guid customerId, Guid listingId, CancellationToken cancellationToken = default)
        => ChangeCartAsync(customerId, cart => cart.RemoveItem(listingId), cancellationToken);

    public Task<Result<CartResponseDto>> ClearCartAsync(
        Guid customerId, CancellationToken cancellationToken = default)
        => ChangeCartAsync(customerId, cart => cart.Clear(), cancellationToken);

    private Task<Result<CartResponseDto>> ChangeCartAsync(
        Guid customerId, Action<Cart> change, CancellationToken cancellationToken)
    {
        return InTransactionAsync("cart:" + customerId, async () =>
        {
            if (!await CustomerIsActiveAsync(customerId, cancellationToken))
                return Result<CartResponseDto>.Failure(ShoppingErrors.AccountUnavailable);
            var cart = await dbContext.Carts.Include(x => x.Items)
                .SingleOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
            if (cart is not null)
            {
                change(cart);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return Result<CartResponseDto>.Success(await MapCartAsync(cart, cancellationToken));
        }, cancellationToken);
    }

    private Task<bool> CustomerIsActiveAsync(Guid customerId, CancellationToken cancellationToken)
        => dbContext.Users.AnyAsync(x => x.Id == customerId && x.IsActive, cancellationToken);

    private static bool IsPurchasable(SellerListing listing)
        => listing.Status == SellerListingStatus.Active && listing.Seller.Status == SellerStatus.Active &&
           listing.ProductVariant.Status == ProductVariantStatus.Active &&
           listing.ProductVariant.Product.Status == ProductStatus.Active;

    private async Task<Dictionary<Guid, SellerListing>> LoadListingsAsync(
        Guid[] listingIds, CancellationToken cancellationToken)
    {
        return await dbContext.SellerListings.AsNoTracking()
            .Include(x => x.Seller).Include(x => x.ProductVariant).ThenInclude(x => x.Product)
            .Where(x => listingIds.Contains(x.Id)).OrderBy(x => x.Id)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    private async Task<Dictionary<Guid, long>> LoadAvailabilityAsync(
        Guid[] listingIds, CancellationToken cancellationToken)
    {
        return await dbContext.InventoryItems.AsNoTracking()
            .Where(x => listingIds.Contains(x.SellerListingId) && x.Warehouse.Status == WarehouseStatus.Active)
            .GroupBy(x => x.SellerListingId)
            .Select(g => new { ListingId = g.Key, Quantity = g.Sum(x => (long)x.OnHandQuantity - x.ReservedQuantity) })
            .ToDictionaryAsync(x => x.ListingId, x => x.Quantity, cancellationToken);
    }

    private async Task<CartResponseDto> MapCartAsync(Cart? cart, CancellationToken cancellationToken)
    {
        if (cart is null)
            return new CartResponseDto(null, null, [], 0, null, false);
        var listingIds = cart.Items.Select(x => x.SellerListingId).ToArray();
        var listings = await LoadListingsAsync(listingIds, cancellationToken);
        var availability = await LoadAvailabilityAsync(listingIds, cancellationToken);
        var items = cart.Items.OrderBy(x => x.SellerListingId).Select(item =>
        {
            if (!listings.TryGetValue(item.SellerListingId, out var listing))
                return new CartItemResponseDto(item.SellerListingId, Guid.Empty, "", "Unavailable listing", "", 0, "", item.Quantity, 0, 0, false);
            var available = availability.GetValueOrDefault(listing.Id);
            return new CartItemResponseDto(listing.Id, listing.SellerId, listing.Seller.DisplayName,
                listing.ProductVariant.Product.Title, listing.ProductVariant.Name, listing.Price.Amount,
                listing.Price.CurrencyCode, item.Quantity, listing.Price.Amount * item.Quantity,
                available, IsPurchasable(listing) && available >= item.Quantity);
        }).ToArray();
        var currencies = items.Select(x => x.CurrencyCode).Distinct(StringComparer.Ordinal).ToArray();
        var total = items.Sum(x => x.LineTotal);
        return new CartResponseDto(cart.Id, cart.RowVersion.Length == 0 ? null : Convert.ToBase64String(cart.RowVersion),
            items, total, currencies.Length == 1 ? currencies[0] : null,
            items.Length > 0 && items.All(x => x.IsAvailable) && currencies.Length == 1 && total <= MaximumTotal);
    }

    // Serialize one customer's cart changes and checkout, including first-cart creation.
    // Serializable isolation protects prices/status read during checkout; rowversion also
    // protects stock. A competing checkout/deadlock becomes a retryable 409, never overselling.
    private async Task<Result<T>> InTransactionAsync<T>(
        string lockName, Func<Task<Result<T>>> operation, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            await AcquireLockAsync(lockName, cancellationToken);
            var result = await operation();
            if (result.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
                return result;
            }
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception exception) when (IsRetryableConflict(exception))
        {
            await RollbackAfterConflictAsync(transaction);
            dbContext.ChangeTracker.Clear();
            return Result<T>.Failure(ShoppingErrors.Conflict(
                "The cart, order or inventory changed concurrently. Reload the cart and retry. For an uncertain checkout response, retry the same Idempotency-Key first."));
        }
    }

    private async Task AcquireLockAsync(string lockName, CancellationToken cancellationToken)
    {
        var result = new SqlParameter("@lockResult", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = "ecommerce:" + lockName };
        await dbContext.Database.ExecuteSqlRawAsync(
            "EXEC @lockResult = sys.sp_getapplock @Resource=@resource, @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=5000;",
            [result, resource], cancellationToken);
        if (result.Value is not int status || status < 0)
            throw new ShoppingLockException();
    }

    private static bool IsRetryableConflict(Exception exception)
        => exception is DbUpdateConcurrencyException or ShoppingLockException ||
           exception is SqlException { Number: 1205 or 1222 } ||
           exception is DbUpdateException { InnerException: SqlException { Number: 1205 or 1222 or 2601 or 2627 } };

    private static async Task RollbackAfterConflictAsync(IDbContextTransaction transaction)
    {
        try { await transaction.RollbackAsync(CancellationToken.None); }
        catch (InvalidOperationException) { /* SQL Server may already have rolled back a deadlock victim. */ }
        catch (SqlException exception) when (exception.Number is 3902 or 3903) { }
    }

    private sealed class ShoppingLockException : Exception;
}
