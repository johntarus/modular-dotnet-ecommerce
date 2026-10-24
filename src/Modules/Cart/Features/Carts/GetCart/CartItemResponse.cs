namespace Cart.Features.Carts.GetCart;

public record CartItemResponse(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal Total);