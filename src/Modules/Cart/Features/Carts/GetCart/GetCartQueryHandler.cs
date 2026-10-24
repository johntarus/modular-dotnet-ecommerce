using Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.GetCart;

internal class GetCartQueryHandler(CartDbContext context) : IRequestHandler<GetCartQuery, CartResponse>
{
    public async Task<CartResponse> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await context.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(
                c => c.UserId == request.UserId,
                cancellationToken);
        if (cart == null)
        {
            return new CartResponse(Guid.Empty, request.UserId, [], 0, DateTime.UtcNow, DateTime.Now);
        }

        var items = cart.Items.Select(item => new CartItemResponse(
            item.ProductId, item.Quantity, item.UnitPrice, item.Quantity * item.UnitPrice)).ToList();

        return new CartResponse(cart.Id, cart.UserId, items, items.Sum(item => item.Total), cart.CreatedAt,
            cart.UpdatedAt);
    }
}