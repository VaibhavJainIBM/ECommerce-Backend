using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.Payment.Application.Abstractions;

namespace ECommerce.Payment.Infrastructure.Orders;

public sealed class OrderPaymentClient(
    HttpClient httpClient,
    IAccessTokenAccessor tokenAccessor)
    : IOrderPaymentClient
{
    public async Task<OrderPaymentContext?> GetPaymentContextAsync(
        Guid orderId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/orders/{orderId}");

        AddAuthorization(request);

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var order =
            await response.Content.ReadFromJsonAsync<OrderWireResponse>(
                cancellationToken);

        if (order is null)
            return null;

        return new OrderPaymentContext(
            order.OrderId,
            customerId,
            order.TotalAmount,
            order.CurrencyCode,
            order.Status,
            order.ExpiresAtUtc);
    }

    public async Task<bool> ConfirmPaymentAsync(
        Guid orderId,
        Guid customerId,
        ConfirmOrderPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"api/orders/{orderId}/payment-confirmation");

        AddAuthorization(message);

        message.Content =
            JsonContent.Create(
                new
                {
                    request.PaymentId,
                    request.Amount,
                    request.CurrencyCode
                });

        using var response =
            await httpClient.SendAsync(
                message,
                cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict ||
            response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();

        return true;
    }

    private void AddAuthorization(
        HttpRequestMessage request)
    {
        var token =
            tokenAccessor.AccessToken;

        if (string.IsNullOrWhiteSpace(token))
            return;

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }

    private sealed record OrderWireResponse(
        Guid OrderId,
        string Status,
        decimal TotalAmount,
        string CurrencyCode,
        DateTimeOffset ExpiresAtUtc);
}