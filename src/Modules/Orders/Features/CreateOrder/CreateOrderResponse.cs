using Orders.Domain.Enums;

namespace Orders.Features.CreateOrder;

public sealed record CreateOrderResponse(
    Guid OrderId,
    OrderStatus Status
);