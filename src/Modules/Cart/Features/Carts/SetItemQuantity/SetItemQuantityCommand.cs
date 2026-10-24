using Cart.Features.Carts.GetCart;
using MediatR;

namespace Cart.Features.Carts.SetItemQuantity;

public record SetItemQuantityCommand(Guid UserId, Guid ProductId, int Quantity) : IRequest;