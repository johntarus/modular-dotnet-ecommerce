using Cart.Features.Carts.CartResolver;
using Cart.Infrastructure.Persistence;
using Inventory.Contracts.Grpc;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Cart.Features.Carts.AddItem;

internal class AddItemToCartCommandHandler(
    CartDbContext context,
    ICartResolver cartResolver,
    InventoryGrpc.InventoryGrpcClient inventoryClient)
    : IRequestHandler<AddItemToCartCommand, Guid>
{
    public async Task<Guid> Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
    {
        // 1. gRPC stock check
        var stockResponse = await inventoryClient.CheckAvailabilityAsync(
            new CheckAvailabilityRequest
            {
                ProductId = request.ProductId.ToString(),
                RequestedQuantity = request.Quantity
            }, cancellationToken: cancellationToken);

        if (!stockResponse.IsAvailable)
            throw new BadHttpRequestException($"Cannot add to cart: {stockResponse.Message}");

        var cart = await cartResolver.ResolveAsync(createIfMissing: true, cancellationToken: cancellationToken)
                   ?? throw new InvalidOperationException(
                       "ICartResolver.ResolveAsync returned null despite createIfMissing: true. " +
                       "This violates the resolver contract — check CartResolver implementation.");

        cart.AddItem(request.ProductId, request.Quantity, 99.99m);
        await context.SaveChangesAsync(cancellationToken);
        return cart.Id;
    }
}