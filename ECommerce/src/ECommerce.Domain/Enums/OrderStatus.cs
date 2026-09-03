namespace ECommerce.Domain.Enums;

public enum OrderStatus
{
    PendingPayment = 1,
    Cancelled = 2,
    Expired = 3,
    Paid = 4,
    PartiallyShipped = 5,
    Shipped = 6
}
