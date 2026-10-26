using MediatR;

namespace Cart.Features.Carts.GetCart;

public record GetCartQuery() : IRequest<CartResponse>;