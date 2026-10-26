using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.CurrentUser;
using Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.RemoveItem;

internal sealed class RemoveItemCommandHandler(CartDbContext context, ICurrentUser user)
    : IRequestHandler<RemoveItemCommand>
{
    public async Task Handle(RemoveItemCommand request, CancellationToken ct)
    {
        var userId = user.UserId
                     ?? throw new ForbiddenException("Sign in to use the cart.");

        var cart = await context.Carts
                       .Include(c => c.Items)
                       .FirstOrDefaultAsync(c => c.UserId == userId, ct)
                   ?? throw new NotFoundException("Cart not found.");

        cart.RemoveItem(request.ProductId);
        await context.SaveChangesAsync(ct);
    }
}