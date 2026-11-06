using Cart.Features.Carts.CartResolver;
using Cart.Infrastructure.Persistence;
using MediatR;

namespace Cart.Features.Carts.RemoveItem;

internal sealed class RemoveItemCommandHandler(CartDbContext context, ICartResolver cartResolver)
    : IRequestHandler<RemoveItemCommand>
{
    public async Task Handle(RemoveItemCommand request, CancellationToken ct)
    {
        // Signed-in user or guest (X-Cart-Token); never creates a cart
        var cart = (await cartResolver.ResolveAsync(createIfMissing: false, ct)).Cart;

        cart.RemoveItem(request.ProductId);
        await context.SaveChangesAsync(ct);
    }
}
