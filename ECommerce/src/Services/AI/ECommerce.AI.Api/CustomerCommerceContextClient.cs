using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace ECommerce.AI.Api;

public sealed partial class CustomerCommerceContextClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<CustomerCommerceContext> LoadAsync(
        string message,
        CancellationToken cancellationToken)
    {
        var authorization = httpContextAccessor.HttpContext?
            .Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorization))
        {
            return new CustomerCommerceContext(
                null,
                null,
                [],
                null,
                false);
        }

        var userResult = await GetAsync<CurrentUserWireResponse>(
            "api/auth/me",
            authorization,
            cancellationToken);

        if (userResult.AuthenticationFailed)
        {
            return new CustomerCommerceContext(
                null,
                null,
                [],
                null,
                true);
        }

        var cartTask = GetAsync<CartWireResponse>(
            "api/cart",
            authorization,
            cancellationToken);

        var ordersTask = GetAsync<PagedOrdersWireResponse>(
            "api/orders?page=1&pageSize=20",
            authorization,
            cancellationToken);

        await Task.WhenAll(cartTask, ordersTask);

        var cartResult = await cartTask;
        var ordersResult = await ordersTask;

        if (cartResult.AuthenticationFailed || ordersResult.AuthenticationFailed)
        {
            return new CustomerCommerceContext(
                null,
                null,
                [],
                null,
                true);
        }

        OrderWireResponse? requestedOrder = null;
        var requestedOrderId = TryReadOrderId(message);

        if (requestedOrderId is not null)
        {
            var requestedOrderResult = await GetAsync<OrderWireResponse>(
                $"api/orders/{requestedOrderId.Value:D}",
                authorization,
                cancellationToken,
                allowNotFound: true);

            if (requestedOrderResult.AuthenticationFailed)
            {
                return new CustomerCommerceContext(
                    null,
                    null,
                    [],
                    null,
                    true);
            }

            requestedOrder = requestedOrderResult.Value;
        }

        return new CustomerCommerceContext(
            userResult.Value,
            cartResult.Value,
            ordersResult.Value?.Items ?? [],
            requestedOrder,
            false);
    }

    public static string BuildPreferenceText(CustomerCommerceContext context)
    {
        if (!context.IsAuthenticated)
        {
            return string.Empty;
        }

        var cartProducts = context.Cart?.Items
            .Take(8)
            .Select(item => $"{item.ProductTitle} {item.VariantName}")
            ?? [];

        var purchasedProducts = context.RecentOrders
            .Where(order =>
                !string.Equals(
                    order.Status,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    order.Status,
                    "Expired",
                    StringComparison.OrdinalIgnoreCase))
            .SelectMany(order => order.Items)
            .Take(12)
            .Select(item => $"{item.ProductTitle} {item.VariantName}");

        return string.Join(
            "; ",
            cartProducts.Concat(purchasedProducts));
    }

    private async Task<DownstreamResult<T>> GetAsync<T>(
        string relativeUrl,
        string authorization,
        CancellationToken cancellationToken,
        bool allowNotFound = false)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            relativeUrl);

        if (!AuthenticationHeaderValue.TryParse(
                authorization,
                out var authenticationHeader))
        {
            return new DownstreamResult<T>(default, true);
        }

        request.Headers.Authorization = authenticationHeader;

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new DownstreamResult<T>(default, true);
        }

        if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
        {
            return new DownstreamResult<T>(default, false);
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"Commerce API returned {(int)response.StatusCode}: " +
                responseBody);
        }

        var value = await response.Content.ReadFromJsonAsync<T>(
            cancellationToken: cancellationToken);

        return new DownstreamResult<T>(value, false);
    }

    private static Guid? TryReadOrderId(string message)
    {
        if (!message.Contains("order", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var match = GuidRegex().Match(message);

        return match.Success && Guid.TryParse(match.Value, out var orderId)
            ? orderId
            : null;
    }

    [GeneratedRegex(
        "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
        RegexOptions.CultureInvariant)]
    private static partial Regex GuidRegex();

    private sealed record DownstreamResult<T>(
        T? Value,
        bool AuthenticationFailed);
}
