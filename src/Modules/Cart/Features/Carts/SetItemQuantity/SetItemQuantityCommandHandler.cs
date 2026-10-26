using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.CurrentUser;
using Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.SetItemQuantity;

internal sealed class SetItemQuantityCommandHandler(CartDbContext context, ICurrentUser user)
    : IRequestHandler<SetItemQuantityCommand>
{
    public async Task Handle(SetItemQuantityCommand request, CancellationToken ct)
    {
        var userId = user.UserId
                     ?? throw new ForbiddenException("Sign in to use the cart.");
        var cart = await context.Carts
                       .Include(c => c.Items)                     
                       .FirstOrDefaultAsync(c => c.UserId == userId, ct)
                   ?? throw new NotFoundException($"Cart for user {userId} was not found.");

        cart.SetItemQuantity(request.ProductId, request.Quantity);
        await context.SaveChangesAsync(ct);
    }
}