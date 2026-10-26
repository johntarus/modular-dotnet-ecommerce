using MediatR;

namespace Cart.Features.Carts.RemoveItem;

public record RemoveItemCommand(Guid ProductId) : IRequest;