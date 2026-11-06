using BuildingBlocks.Exceptions;
using Cart.Features.Carts.CartResolver;
using Cart.Infrastructure.Persistence;
using Inventory.Contracts.Grpc;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Cart.Features.Carts.SetItemQuantity;

internal sealed class SetItemQuantityCommandHandler(
    CartDbContext context,
    ICartResolver cartResolver,
    InventoryGrpc.InventoryGrpcClient inventoryClient)
    : IRequestHandler<SetItemQuantityCommand>
{
    public async Task Handle(SetItemQuantityCommand request, CancellationToken ct)
    {
        // Signed-in user or guest (X-Cart-Token); never creates a cart
        var cart = (await cartResolver.ResolveAsync(createIfMissing: false, ct)).Cart;

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
