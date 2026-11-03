using BuildingBlocks.Infrastructure.CurrentUser;
using Cart.Features.Carts.CartResolver;
using Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.GetCart;

internal sealed class GetCartQueryHandler(CartDbContext context, ICartResolver cartResolver)
    : IRequestHandler<GetCartQuery, CartResponse>
{
    public async Task<CartResponse> Handle(GetCartQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var cart = await cartResolver.ResolveAsync(createIfMissing: true, ct);

        if (cart is null || cart.Items.Count == 0)
            return new CartResponse(cart?.Id ?? Guid.Empty,
                cart?.UserId, [], 0m, cart?.CreatedAt ?? now,
                cart?.UpdatedAt ?? now);

        var items = cart.Items
            .Select(i => new CartItemResponse(i.ProductId, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice))
            .ToList();

        return new CartResponse(cart.Id, cart.UserId, items, items.Sum(i => i.Total),
            cart.CreatedAt, cart.UpdatedAt);
    }
}