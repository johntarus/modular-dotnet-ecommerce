using Cart.Features.Carts.GetCart;
using MediatR;

namespace Cart.Features.Carts.SetItemQuantity;

public record SetItemQuantityCommand(Guid ProductId, int Quantity) : IRequest<SetItemQuantityCommandValidator>, IRequest;