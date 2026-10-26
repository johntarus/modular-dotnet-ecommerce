using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.CurrentUser;
using Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.ClearCart;

internal class ClearCartCommandHandler(CartDbContext context, ICurrentUser user) : IRequestHandler<ClearCartCommand>
{
    public async Task Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var userId = user.UserId
                     ?? throw new ForbiddenException("Sign in to use the cart.");
        var cart = await context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        
        if(cart == null)
            throw new NotFoundException("Cart not found");
        cart.Clear();
        await context.SaveChangesAsync(cancellationToken);
    }
}