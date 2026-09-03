using ECommerce.Application.Common;
using ECommerce.Application.Payments;
using ECommerce.Application.Shopping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed partial class ShoppingRepository
{
    public Task<Result<CreatePaymentResponseDto>> CreateDemoPaymentAsync(
        Guid customerId, Guid orderId, Guid requestKey, CancellationToken cancellationToken = default)
    {
        return InTransactionAsync("order:" + orderId, async () =>
        {
            if (!await CustomerIsActiveAsync(customerId, cancellationToken))
                return Result<CreatePaymentResponseDto>.Failure(ShoppingErrors.AccountUnavailable);
            var order = await dbContext.Orders.SingleOrDefaultAsync(
                x => x.Id == orderId && x.CustomerId == customerId, cancellationToken);
            if (order is null)
                return Result<CreatePaymentResponseDto>.Failure(ShoppingErrors.NotFound("The order was not found."));
            var existing = await dbContext.DemoPayments.SingleOrDefaultAsync(
                x => x.OrderId == orderId && x.RequestKey == requestKey, cancellationToken);
            if (existing is not null)
                return Result<CreatePaymentResponseDto>.Success(new(MapPayment(existing, order), true));
            if (order.Status != OrderStatus.PendingPayment || order.ExpiresAtUtc <= DateTimeOffset.UtcNow)
                return Result<CreatePaymentResponseDto>.Failure(ShoppingErrors.Conflict("Only an unexpired PendingPayment order can start a payment."));
            if (await dbContext.DemoPayments.AnyAsync(
                    x => x.OrderId == orderId && x.Status == DemoPaymentStatus.Created, cancellationToken))
                return Result<CreatePaymentResponseDto>.Failure(ShoppingErrors.Conflict("This order already has an open payment attempt. Get the order's payments and complete that attempt."));
            var payment = new DemoPayment(order.Id, requestKey, order.TotalAmount, order.CurrencyCode);
            dbContext.DemoPayments.Add(payment);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<CreatePaymentResponseDto>.Success(new(MapPayment(payment, order), false));
        }, cancellationToken);
    }

    public async Task<Result<PaymentResponseDto>> GetDemoPaymentAsync(
        Guid customerId, Guid paymentId, CancellationToken cancellationToken = default)
    {
        if (!await CustomerIsActiveAsync(customerId, cancellationToken))
            return Result<PaymentResponseDto>.Failure(ShoppingErrors.AccountUnavailable);
        var payment = await dbContext.DemoPayments.AsNoTracking().Include(x => x.Order)
            .SingleOrDefaultAsync(x => x.Id == paymentId && x.Order.CustomerId == customerId, cancellationToken);
        return payment is null
            ? Result<PaymentResponseDto>.Failure(ShoppingErrors.NotFound("The payment was not found."))
            : Result<PaymentResponseDto>.Success(MapPayment(payment, payment.Order));
    }

    public async Task<Result<IReadOnlyList<PaymentResponseDto>>> GetDemoPaymentsAsync(
        Guid customerId, Guid orderId, CancellationToken cancellationToken = default)
    {
        if (!await CustomerIsActiveAsync(customerId, cancellationToken))
            return Result<IReadOnlyList<PaymentResponseDto>>.Failure(ShoppingErrors.AccountUnavailable);
        var order = await dbContext.Orders.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == orderId && x.CustomerId == customerId, cancellationToken);
        if (order is null)
            return Result<IReadOnlyList<PaymentResponseDto>>.Failure(ShoppingErrors.NotFound("The order was not found."));
        var payments = await dbContext.DemoPayments.AsNoTracking().Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).ToArrayAsync(cancellationToken);
        return Result<IReadOnlyList<PaymentResponseDto>>.Success(payments.Select(x => MapPayment(x, order)).ToArray());
    }

    public async Task<Result<PaymentResponseDto>> CompleteDemoPaymentAsync(
        Guid customerId, Guid paymentId, bool succeeded, CancellationToken cancellationToken = default)
    {
        // Resolve only a caller-owned ID before taking the same order lock used by cancellation,
        // expiration and shipment. The protected record is re-read inside that transaction.
        var orderId = await dbContext.DemoPayments.AsNoTracking()
            .Where(x => x.Id == paymentId && x.Order.CustomerId == customerId)
            .Select(x => (Guid?)x.OrderId).SingleOrDefaultAsync(cancellationToken);
        if (!orderId.HasValue)
            return Result<PaymentResponseDto>.Failure(ShoppingErrors.NotFound("The payment was not found."));
        return await InTransactionAsync("order:" + orderId.Value, async () =>
        {
            if (!await CustomerIsActiveAsync(customerId, cancellationToken))
                return Result<PaymentResponseDto>.Failure(ShoppingErrors.AccountUnavailable);
            var payment = await dbContext.DemoPayments.Include(x => x.Order).ThenInclude(x => x.Items)
                .SingleOrDefaultAsync(x => x.Id == paymentId && x.Order.CustomerId == customerId, cancellationToken);
            if (payment is null)
                return Result<PaymentResponseDto>.Failure(ShoppingErrors.NotFound("The payment was not found."));
            var order = payment.Order;
            var target = succeeded ? DemoPaymentStatus.Succeeded : DemoPaymentStatus.Failed;
            if (payment.Status == target)
                return Result<PaymentResponseDto>.Success(MapPayment(payment, order));
            if (payment.Status != DemoPaymentStatus.Created)
                return Result<PaymentResponseDto>.Failure(ShoppingErrors.Conflict("This payment attempt already has a different final outcome."));
            var now = DateTimeOffset.UtcNow;
            if (order.Status != OrderStatus.PendingPayment || order.ExpiresAtUtc <= now)
                return Result<PaymentResponseDto>.Failure(ShoppingErrors.Conflict("This order is no longer awaiting payment or its reservation has expired."));
            if (payment.Amount != order.TotalAmount || payment.CurrencyCode != order.CurrencyCode)
                return Result<PaymentResponseDto>.Failure(ShoppingErrors.Conflict("Payment amount and currency must match the server's order snapshot."));
            payment.Complete(succeeded, now);
            if (succeeded) order.MarkPaid(now);
            // Successful payment keeps reservations; seller shipment consumes them later.
            // Failed attempts leave the order pending so the customer can retry before expiry.
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<PaymentResponseDto>.Success(MapPayment(payment, order));
        }, cancellationToken);
    }

    private async Task CancelPendingPaymentsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var payments = await dbContext.DemoPayments
            .Where(x => x.OrderId == orderId && x.Status == DemoPaymentStatus.Created)
            .ToArrayAsync(cancellationToken);
        foreach (var payment in payments) payment.Cancel(DateTimeOffset.UtcNow);
    }

    private static PaymentResponseDto MapPayment(DemoPayment payment, Order order) => new(
        payment.Id, payment.OrderId, "Demo", payment.Status.ToString(), payment.Amount,
        payment.CurrencyCode, order.Status.ToString(), payment.CreatedAtUtc, payment.CompletedAtUtc,
        Convert.ToBase64String(payment.RowVersion));
}
