using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.CurrentUser;
using Cart.Infrastructure.Persistence;
using Inventory.Contracts.Grpc;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.SetItemQuantity;

internal sealed class SetItemQuantityCommandHandler(
    CartDbContext context,
    ICurrentUser user,
    InventoryGrpc.InventoryGrpcClient inventoryClient)
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

        // Fail with 404 for a missing item before asking Inventory about it
        if (cart.Items.All(i => i.ProductId != request.ProductId))
            throw new NotFoundException("The product does not exist in the cart.");

        // The new quantity is the total for this product, so check it directly
        var stockResponse = await inventoryClient.CheckAvailabilityAsync(
            new CheckAvailabilityRequest
            {
                ProductId = request.ProductId.ToString(),
                RequestedQuantity = request.Quantity
            }, cancellationToken: ct);

        if (!stockResponse.IsAvailable)
            throw new BadHttpRequestException($"Cannot update quantity: {stockResponse.Message}");

        cart.SetItemQuantity(request.ProductId, request.Quantity);
        await context.SaveChangesAsync(ct);
    }
}