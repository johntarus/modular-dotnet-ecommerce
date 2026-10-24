using MediatR;

namespace Cart.Features.Carts.ClearCart;

public record ClearCartCommand(Guid UserId): IRequest;