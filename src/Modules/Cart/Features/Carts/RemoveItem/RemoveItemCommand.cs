using MediatR;

namespace Cart.Features.Carts.RemoveItem;

public record RemoveItemCommand(Guid UserId, Guid ProductId) : IRequest;