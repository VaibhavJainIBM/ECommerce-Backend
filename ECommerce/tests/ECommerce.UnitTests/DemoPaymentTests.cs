using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.UnitTests;

public sealed class DemoPaymentTests
{
    [Fact]
    public void Successful_attempt_replays_without_changing_time_and_cannot_become_failed()
    {
        var payment = NewPayment();
        var now = DateTimeOffset.UtcNow;
        payment.Complete(true, now);
        payment.Complete(true, now.AddMinutes(1));
        Assert.Equal(DemoPaymentStatus.Succeeded, payment.Status);
        Assert.Equal(now, payment.CompletedAtUtc);
        Assert.Throws<InvalidOperationException>(() => payment.Complete(false, now));
        payment.Cancel(now.AddMinutes(2));
        Assert.Equal(DemoPaymentStatus.Succeeded, payment.Status);
    }

    [Fact]
    public void Failed_attempt_replays_without_changing_time_and_cannot_become_successful()
    {
        var payment = NewPayment();
        var now = DateTimeOffset.UtcNow;
        payment.Complete(false, now);
        payment.Complete(false, now.AddMinutes(1));
        Assert.Equal(DemoPaymentStatus.Failed, payment.Status);
        Assert.Equal(now, payment.CompletedAtUtc);
        Assert.Throws<InvalidOperationException>(() => payment.Complete(true, now));
    }

    [Fact]
    public void Cancelled_attempt_cannot_be_completed_and_cancel_is_idempotent()
    {
        var payment = NewPayment();
        var now = DateTimeOffset.UtcNow;
        payment.Cancel(now);
        payment.Cancel(now.AddMinutes(1));
        Assert.Equal(DemoPaymentStatus.Cancelled, payment.Status);
        Assert.Equal(now, payment.CompletedAtUtc);
        Assert.Throws<InvalidOperationException>(() => payment.Complete(true, now));
        Assert.Throws<InvalidOperationException>(() => payment.Complete(false, now));
    }

    [Fact]
    public void Expired_or_cancelled_order_cannot_be_paid()
    {
        var now = DateTimeOffset.UtcNow;
        var expired = NewOrder(now);
        Assert.Throws<InvalidOperationException>(() => expired.MarkPaid(now));
        Assert.Equal(OrderStatus.PendingPayment, expired.Status);
        Assert.Null(expired.PaidAtUtc);
        var cancelled = NewOrder(now.AddMinutes(30));
        cancelled.Cancel();
        Assert.Throws<InvalidOperationException>(() => cancelled.MarkPaid(now));
        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public void Paid_order_is_explicitly_demo_and_cannot_expire_cancel_or_be_paid_twice()
    {
        var now = DateTimeOffset.UtcNow;
        var order = NewOrder(now.AddMinutes(30));
        order.MarkPaid(now);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal("Demo", order.PaymentMode);
        Assert.Equal(now, order.PaidAtUtc);
        Assert.False(order.Expire(now.AddHours(1)));
        Assert.Throws<InvalidOperationException>(() => order.Cancel());
        Assert.Throws<InvalidOperationException>(() => order.MarkPaid(now));
    }

    private static DemoPayment NewPayment() => new(Guid.NewGuid(), Guid.NewGuid(), 100, "INR");

    private static Order NewOrder(DateTimeOffset expiry)
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), new string('A', 64), "Demo Customer", "9999999999",
            new Address("Main Road", "Delhi", "Delhi", "110001", "IN"), expiry);
        order.AddItem(new OrderItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Demo Seller", "Phone", "Black", "DEMO-SKU", 100, "INR", 1));
        return order;
    }
}
