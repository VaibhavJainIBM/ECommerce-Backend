using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Application.Shopping;

public sealed class ShoppingService(
    IShoppingRepository repository,
    ICurrentUser currentUser) : IShoppingService
{
    public Task<Result<CartResponseDto>> GetCartAsync(CancellationToken cancellationToken = default) =>
        ForUser(id => repository.GetCartAsync(id, cancellationToken));

    public Task<Result<CartResponseDto>> SetCartItemAsync(
        Guid listingId, SetCartItemRequestDto? request, CancellationToken cancellationToken = default) =>
        ForUser(id =>
        {
            if (listingId == Guid.Empty)
                return Invalid<CartResponseDto>("Listing ID is required.");
            if (request is null || request.Quantity is < 1 or > 99)
                return Invalid<CartResponseDto>("Quantity must be between 1 and 99. Use DELETE to remove an item.");
            return repository.SetCartItemAsync(id, listingId, request.Quantity, cancellationToken);
        });

    public Task<Result<CartResponseDto>> RemoveCartItemAsync(
        Guid listingId, CancellationToken cancellationToken = default) =>
        ForUser(id => listingId == Guid.Empty
            ? Invalid<CartResponseDto>("Listing ID is required.")
            : repository.RemoveCartItemAsync(id, listingId, cancellationToken));

    public Task<Result<CartResponseDto>> ClearCartAsync(CancellationToken cancellationToken = default) =>
        ForUser(id => repository.ClearCartAsync(id, cancellationToken));

    public Task<Result<CheckoutResponseDto>> CheckoutAsync(
        string? idempotencyKey, CheckoutRequestDto? request, CancellationToken cancellationToken = default) =>
        ForUser(customerId =>
        {
            if (!Guid.TryParse(idempotencyKey, out var key) || key == Guid.Empty)
                return Invalid<CheckoutResponseDto>("Idempotency-Key must be a nonempty GUID. Reuse the same key and body only when retrying the same checkout.");
            if (request is null)
                return Invalid<CheckoutResponseDto>("Checkout details are required.");

            var errors = new List<Error>();
            var rowVersion = new byte[8];
            if (request.CartRowVersion is null ||
                !Convert.TryFromBase64String(request.CartRowVersion.Trim(), rowVersion, out var written) || written != 8)
                errors.Add(ShoppingErrors.Validation("CartRowVersion must be the cart's current 8-byte Base64 row version."));

            if (request.ExpectedTotalAmount <= 0 || request.ExpectedTotalAmount > 9999999999999999.99m ||
                decimal.Round(request.ExpectedTotalAmount, 2) != request.ExpectedTotalAmount)
                errors.Add(ShoppingErrors.Validation("ExpectedTotalAmount must be positive, at most 9999999999999999.99, and have at most two decimal places."));

            var rawCurrency = request.CurrencyCode?.Trim();
            var currency = rawCurrency?.ToUpperInvariant();
            if (!AsciiLetters(rawCurrency, 3))
                errors.Add(ShoppingErrors.Validation("CurrencyCode must contain three ASCII letters, such as INR."));

            var recipient = Required(request.RecipientName, "RecipientName", 150, errors);
            var phone = Required(request.Phone, "Phone", 32, errors);
            var shipping = request.ShippingAddress;
            if (shipping is null)
                errors.Add(ShoppingErrors.Validation("ShippingAddress is required."));
            var line1 = Required(shipping?.Line1, "ShippingAddress.Line1", 200, errors);
            var line2 = string.IsNullOrWhiteSpace(shipping?.Line2) ? null : shipping.Line2.Trim();
            if (line2?.Length > 200)
                errors.Add(ShoppingErrors.Validation("ShippingAddress.Line2 cannot exceed 200 characters."));
            var city = Required(shipping?.City, "ShippingAddress.City", 100, errors);
            var state = Required(shipping?.StateOrProvince, "ShippingAddress.StateOrProvince", 100, errors);
            var postal = Required(shipping?.PostalCode, "ShippingAddress.PostalCode", 20, errors);
            var rawCountry = shipping?.CountryCode?.Trim();
            var country = rawCountry?.ToUpperInvariant();
            if (!AsciiLetters(rawCountry, 2))
                errors.Add(ShoppingErrors.Validation("ShippingAddress.CountryCode must contain two ASCII letters, such as IN."));

            if (errors.Count > 0)
                return Task.FromResult(Result<CheckoutResponseDto>.Failure(errors));

            var address = new Address(line1, city, state, postal, country!, line2);
            // Fixed property order and normalized values make semantically identical retries stable.
            var canonicalBytes = JsonSerializer.SerializeToUtf8Bytes(new
            {
                CartRowVersion = Convert.ToBase64String(rowVersion),
                ExpectedTotalAmount = request.ExpectedTotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                CurrencyCode = currency,
                RecipientName = recipient,
                Phone = phone,
                ShippingAddress = new { address.Line1, address.Line2, address.City,
                    address.StateOrProvince, address.PostalCode, address.CountryCode }
            });
            var requestHash = Convert.ToHexString(SHA256.HashData(canonicalBytes));
            return repository.CheckoutAsync(new CheckoutCommand(
                customerId, key, requestHash, rowVersion, request.ExpectedTotalAmount,
                currency!, recipient, phone, address), cancellationToken);
        });

    public Task<Result<PagedOrdersResponseDto>> GetOrdersAsync(
        OrderQueryDto? query, CancellationToken cancellationToken = default) =>
        ForUser(id =>
        {
            query ??= new OrderQueryDto();
            return ValidPage(query)
                ? repository.GetOrdersAsync(id, query.Page, query.PageSize, cancellationToken)
                : Invalid<PagedOrdersResponseDto>("Page must be positive, PageSize must be 1..100, and the offset must not exceed Int32.MaxValue.");
        });

    public Task<Result<OrderResponseDto>> GetOrderAsync(
        Guid orderId, CancellationToken cancellationToken = default) =>
        ForUser(id => orderId == Guid.Empty
            ? Invalid<OrderResponseDto>("Order ID is required.")
            : repository.GetOrderAsync(id, orderId, cancellationToken));

    public Task<Result<OrderResponseDto>> CancelOrderAsync(
        Guid orderId, CancellationToken cancellationToken = default) =>
        ForUser(id => orderId == Guid.Empty
            ? Invalid<OrderResponseDto>("Order ID is required.")
            : repository.CancelOrderAsync(id, orderId, cancellationToken));

    public Task<Result<PagedSellerOrdersResponseDto>> GetSellerOrdersAsync(
        Guid sellerId, OrderQueryDto? query, CancellationToken cancellationToken = default) =>
        ForUser(_ =>
        {
            if (sellerId == Guid.Empty)
                return Invalid<PagedSellerOrdersResponseDto>("Seller ID is required.");
            query ??= new OrderQueryDto();
            return ValidPage(query)
                ? repository.GetSellerOrdersAsync(sellerId, query.Page, query.PageSize, cancellationToken)
                : Invalid<PagedSellerOrdersResponseDto>("Page must be positive, PageSize must be 1..100, and the offset must not exceed Int32.MaxValue.");
        });

    private Task<Result<T>> ForUser<T>(Func<Guid, Task<Result<T>>> operation)
    {
        var id = currentUser.UserId;
        return !id.HasValue || id == Guid.Empty
            ? Task.FromResult(Result<T>.Failure(ShoppingErrors.Unauthenticated))
            : operation(id.Value);
    }

    private static Task<Result<T>> Invalid<T>(string message) =>
        Task.FromResult(Result<T>.Failure(ShoppingErrors.Validation(message)));

    private static bool ValidPage(OrderQueryDto query) =>
        query.Page > 0 && query.PageSize is >= 1 and <= 100 &&
        ((long)query.Page - 1) * query.PageSize <= int.MaxValue;

    private static bool AsciiLetters(string? value, int count) =>
        value?.Length == count && value.All(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z');

    private static string Required(string? value, string field, int maxLength, List<Error> errors)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maxLength)
            errors.Add(ShoppingErrors.Validation($"{field} is required and cannot exceed {maxLength} characters."));
        return normalized;
    }
}
