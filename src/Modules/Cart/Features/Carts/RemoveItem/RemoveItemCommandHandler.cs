using BuildingBlocks.Exceptions;
using Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.RemoveItem;

internal class RemoveItemCommandHandler(CartDbContext context) : IRequestHandler<RemoveItemCommand>
{
    public async Task Handle(RemoveItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);
        
        if(cart == null)
            throw new NotFoundException("cart not found");
        
        cart.RemoveItem(request.ProductId);
        await context.SaveChangesAsync(cancellationToken);
    }
}