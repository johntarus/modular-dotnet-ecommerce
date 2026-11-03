using BuildingBlocks.Infrastructure.CurrentUser;
using Cart.Features.Carts.CartResolver;
using Cart.Infrastructure.Persistence;
using MediatR;

namespace Cart.Features.Carts.GetCart;

internal sealed class GetCartQueryHandler(
    CartDbContext context,
    ICartResolver cartResolver,
    ICurrentUser currentUser)
    : IRequestHandler<GetCartQuery, CartResponse>
{
    public async Task<CartResponse> Handle(GetCartQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // A guest with no token simply has no cart yet — that's an empty cart, not an error.
        var isAuthenticated = currentUser.UserId is { } userId && userId != Guid.Empty;
        if (!isAuthenticated && cartResolver.GetGuestToken() is null)
            return new CartResponse(Guid.Empty, null, [], 0m, now, now);

        var resolved = await cartResolver.ResolveAsync(createIfMissing: false, ct);
        var cart = resolved.Cart;
        if (cart.Items.Count == 0)
            return new CartResponse(cart.Id, cart.UserId, [], 0m, cart.CreatedAt, cart.UpdatedAt);

        var items = cart.Items
            .Select(i => new CartItemResponse(i.ProductId, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)).ToList();

        return new CartResponse(
            cart.Id,
            cart.UserId,
            items, items.Sum(i => i.Total),
            cart.CreatedAt,
            cart.UpdatedAt);
    }
}