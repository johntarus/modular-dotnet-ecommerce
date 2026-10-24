using BuildingBlocks.Exceptions;
using Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Cart.Features.Carts.SetItemQuantity;

internal class SetItemQuantityCommandHandler(CartDbContext context) : IRequestHandler<SetItemQuantityCommand>
{
    public async Task Handle(SetItemQuantityCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");

        var cart = await context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);
        
        if (cart == null)
            throw new NotFoundException($"Cart with id {request.UserId} does not exist.");
        
        cart.SetItemQuantity(request.ProductId, request.Quantity);
        await context.SaveChangesAsync(cancellationToken);
    }
}