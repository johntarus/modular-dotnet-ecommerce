namespace Cart.Features.Carts.AddItem;

public record AddItemToCartRequest(Guid UserId, Guid ProductId, int Quantity);
