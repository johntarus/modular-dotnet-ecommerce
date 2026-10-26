using BuildingBlocks.Infrastructure.CurrentUser;
using Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.GetCart;

internal sealed class GetCartQueryHandler(CartDbContext context, ICurrentUser user)
    : IRequestHandler<GetCartQuery, CartResponse>
{
    public async Task<CartResponse> Handle(GetCartQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // No signed-in (or debug) user: an empty cart, not an error
        if (user.UserId is not { } userId)
            return new CartResponse(Guid.Empty, Guid.Empty, [], 0m, now, now);

        var cart = await context.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is null)
            return new CartResponse(Guid.Empty, userId, [], 0m, now, now);

        var items = cart.Items
            .Select(i => new CartItemResponse(i.ProductId, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice))
            .ToList();

        return new CartResponse(cart.Id, cart.UserId, items, items.Sum(i => i.Total),
            cart.CreatedAt, cart.UpdatedAt);
    }
}