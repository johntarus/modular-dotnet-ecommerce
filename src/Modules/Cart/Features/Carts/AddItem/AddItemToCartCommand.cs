using MediatR;

namespace Cart.Features.Carts.AddItem;

public record AddItemToCartCommand(
    Guid UserId,
    Guid ProductId,
    int Quantity
) : IRequest<Guid>;