namespace Orders.Domain.Enums;

public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    PaymentFailed = 7,
    Refunded = 8,
    AwaitingPayment = 9
}