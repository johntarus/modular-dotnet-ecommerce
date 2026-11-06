using Cart.Features.Carts.CartResolver;
using Cart.Infrastructure.Persistence;
using MediatR;

namespace Cart.Features.Carts.ClearCart;

internal class ClearCartCommandHandler(CartDbContext context, ICartResolver cartResolver) : IRequestHandler<ClearCartCommand>
{
    public async Task Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        // Signed-in user or guest (X-Cart-Token); never creates a cart
        var cart = (await cartResolver.ResolveAsync(createIfMissing: false, cancellationToken)).Cart;

        cart.Clear();
        await context.SaveChangesAsync(cancellationToken);
    }
}
