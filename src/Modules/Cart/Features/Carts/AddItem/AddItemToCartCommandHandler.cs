using BuildingBlocks.Exceptions;
using Cart.Domain.Entities;
using Cart.Infrastructure.Persistence;
using Inventory.Contracts.Grpc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.AddItem;

internal class AddItemToCartCommandHandler(CartDbContext context, InventoryGrpc.InventoryGrpcClient inventoryClient)
    : IRequestHandler<AddItemToCartCommand, Guid>
{
    public async Task<Guid> Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
    {
        // 1. SYNCHRONOUS gRPC CALL: Check if Inventory has enough stock
        var stockResponse = await inventoryClient.CheckAvailabilityAsync(
            new CheckAvailabilityRequest
            {
                ProductId = request.ProductId.ToString(),
                RequestedQuantity = request.Quantity
            }, cancellationToken: cancellationToken);

        if (!stockResponse.IsAvailable)
        {
            throw new NotFoundException($"Cannot add to cart: {stockResponse.Message}");
        }

        // 2. Find or create the user's cart
        var cart = await context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart == null)
        {
            cart = ShoppingCart.Create(request.UserId);
            context.Carts.Add(cart);
        }
        
        cart.AddItem(request.ProductId, request.Quantity, 99);
        await context.SaveChangesAsync(cancellationToken);
        return cart.Id;
    }
}