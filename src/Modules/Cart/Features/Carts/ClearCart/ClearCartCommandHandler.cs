using BuildingBlocks.Exceptions;
using Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.ClearCart;

internal class ClearCartCommandHandler(CartDbContext context) : IRequestHandler<ClearCartCommand>
{
    public async Task Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);
        
        if(cart == null)
            throw new NotFoundException("Cart not found");
        cart.Clear();
        await context.SaveChangesAsync(cancellationToken);
    }
}