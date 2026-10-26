namespace Cart.Features.Carts.AddItem;

public record AddItemToCartRequest(Guid ProductId, int Quantity);
