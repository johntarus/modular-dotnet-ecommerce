namespace Orders.Features.CreateOrder;

// Items come from the cart and shipping is calculated server-side.
public sealed record CreateOrderRequest(
    Guid CartId,
    Guid AddressId);
