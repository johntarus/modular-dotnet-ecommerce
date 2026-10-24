using MediatR;

namespace Cart.Features.Carts.GetCart;

public record GetCartQuery(Guid UserId ) : IRequest<CartResponse>;