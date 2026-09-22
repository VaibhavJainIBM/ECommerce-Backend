using ECommerce.Payment.Application.Abstractions;
using ECommerce.Payment.Application.Common;
using ECommerce.Payment.Application.Contracts;
using ECommerce.Payment.Domain.Entities;
using ECommerce.Payment.Domain.Enums;

namespace ECommerce.Payment.Application.Services;

public sealed class PaymentService(
    IPaymentRepository repository,
    IOrderPaymentClient orderClient,
    ICurrentUser currentUser)
    : IPaymentService
{
    public async Task<Result<CreatePaymentResponse>> CreateAsync(
        Guid orderId,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.UserId;

        if (!customerId.HasValue ||
            customerId.Value == Guid.Empty)
        {
            return Result<CreatePaymentResponse>.Failure(
                PaymentErrors.Unauthenticated);
        }

        if (orderId == Guid.Empty ||
            !Guid.TryParse(
                idempotencyKey,
                out var requestKey) ||
            requestKey == Guid.Empty)
        {
            return Result<CreatePaymentResponse>.Failure(
                PaymentErrors.Validation(
                    "Order ID and a GUID Idempotency-Key are required."));
        }

        var existing =
            await repository.GetByOrderAndRequestKeyAsync(
                orderId,
                requestKey,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.CustomerId != customerId.Value)
            {
                return Result<CreatePaymentResponse>.Failure(
                    PaymentErrors.NotFound(
                        "The payment was not found."));
            }

            return Result<CreatePaymentResponse>.Success(
                new CreatePaymentResponse(
                    Map(existing),
                    true));
        }

        OrderPaymentContext? order;

        try
        {
            order =
                await orderClient.GetPaymentContextAsync(
                    orderId,
                    customerId.Value,
                    cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result<CreatePaymentResponse>.Failure(
                PaymentErrors.Dependency(
                    "Order service is unavailable."));
        }

        if (order is null)
        {
            return Result<CreatePaymentResponse>.Failure(
                PaymentErrors.NotFound(
                    "The order was not found."));
        }

        if (!string.Equals(
                order.Status,
                "PendingPayment",
                StringComparison.Ordinal) ||
            order.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return Result<CreatePaymentResponse>.Failure(
                PaymentErrors.Conflict(
                    "Only an unexpired PendingPayment order can start a payment."));
        }

        var hasOpenPayment =
            await repository.HasOpenPaymentAsync(
                orderId,
                cancellationToken);

        if (hasOpenPayment)
        {
            return Result<CreatePaymentResponse>.Failure(
                PaymentErrors.Conflict(
                    "This order already has an open payment attempt."));
        }

        var payment =
            new PaymentAttempt(
                order.OrderId,
                customerId.Value,
                requestKey,
                order.TotalAmount,
                order.CurrencyCode);

        await repository.AddAsync(
            payment,
            cancellationToken);

        await repository.SaveChangesAsync(
            cancellationToken);

        return Result<CreatePaymentResponse>.Success(
            new CreatePaymentResponse(
                Map(payment),
                false));
    }

    public async Task<Result<PaymentResponse>> GetAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.UserId;

        if (!customerId.HasValue)
        {
            return Result<PaymentResponse>.Failure(
                PaymentErrors.Unauthenticated);
        }

        var payment =
            await repository.GetByIdAsync(
                paymentId,
                cancellationToken);

        if (payment is null ||
            payment.CustomerId != customerId.Value)
        {
            return Result<PaymentResponse>.Failure(
                PaymentErrors.NotFound(
                    "The payment was not found."));
        }

        return Result<PaymentResponse>.Success(
            Map(payment));
    }

    public async Task<Result<IReadOnlyList<PaymentResponse>>>
        GetForOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.UserId;

        if (!customerId.HasValue)
        {
            return Result<IReadOnlyList<PaymentResponse>>
                .Failure(
                    PaymentErrors.Unauthenticated);
        }

        var payments =
            await repository.GetByOrderAsync(
                orderId,
                customerId.Value,
                cancellationToken);

        return Result<IReadOnlyList<PaymentResponse>>
            .Success(
                payments
                    .Select(Map)
                    .ToArray());
    }

    public async Task<Result<PaymentResponse>> CompleteAsync(
        Guid paymentId,
        CompletePaymentRequest? request,
        CancellationToken cancellationToken = default)
    {
        var customerId = currentUser.UserId;

        if (!customerId.HasValue)
        {
            return Result<PaymentResponse>.Failure(
                PaymentErrors.Unauthenticated);
        }

        var outcome = request?.Outcome?.Trim();

        var succeeded =
            string.Equals(
                outcome,
                "Succeeded",
                StringComparison.OrdinalIgnoreCase);

        var failed =
            string.Equals(
                outcome,
                "Failed",
                StringComparison.OrdinalIgnoreCase);

        if (paymentId == Guid.Empty ||
            (!succeeded && !failed))
        {
            return Result<PaymentResponse>.Failure(
                PaymentErrors.Validation(
                    "Outcome must be Succeeded or Failed."));
        }

        var payment =
            await repository.GetByIdAsync(
                paymentId,
                cancellationToken);

        if (payment is null ||
            payment.CustomerId != customerId.Value)
        {
            return Result<PaymentResponse>.Failure(
                PaymentErrors.NotFound(
                    "The payment was not found."));
        }

        var target =
            succeeded
                ? PaymentStatus.Succeeded
                : PaymentStatus.Failed;

        if (payment.Status == target)
        {
            return Result<PaymentResponse>.Success(
                Map(payment));
        }

        if (payment.Status != PaymentStatus.Created)
        {
            return Result<PaymentResponse>.Failure(
                PaymentErrors.Conflict(
                    "The payment already has a different final outcome."));
        }

        if (succeeded)
        {
            bool confirmed;

            try
            {
                confirmed =
                    await orderClient.ConfirmPaymentAsync(
                        payment.OrderId,
                        customerId.Value,
                        new ConfirmOrderPaymentRequest(
                            payment.Id,
                            payment.Amount,
                            payment.CurrencyCode),
                        cancellationToken);
            }
            catch (HttpRequestException)
            {
                return Result<PaymentResponse>.Failure(
                    PaymentErrors.Dependency(
                        "Order service is unavailable."));
            }

            if (!confirmed)
            {
                return Result<PaymentResponse>.Failure(
                    PaymentErrors.Conflict(
                        "The order rejected this payment."));
            }
        }

        payment.Complete(
            succeeded,
            DateTimeOffset.UtcNow);

        await repository.SaveChangesAsync(
            cancellationToken);

        return Result<PaymentResponse>.Success(
            Map(payment));
    }

    private static PaymentResponse Map(
        PaymentAttempt payment)
    {
        return new PaymentResponse(
            payment.Id,
            payment.OrderId,
            payment.Status.ToString(),
            payment.Amount,
            payment.CurrencyCode,
            payment.CreatedAtUtc,
            payment.CompletedAtUtc);
    }
}