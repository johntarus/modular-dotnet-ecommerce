namespace Cart.Features.Carts.GetCart;

public record CartResponse(
    Guid Id,
    Guid? UserId,
    List<CartItemResponse> Items,
    decimal Subtotal,
    DateTime CreatedAt,
    DateTime UpdatedAt);