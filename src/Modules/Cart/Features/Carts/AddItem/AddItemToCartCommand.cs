using MediatR;

namespace Cart.Features.Carts.AddItem;

public record AddItemToCartCommand(
    Guid ProductId,
    int Quantity
) : IRequest<AddItemResult>;