using Cart.Features.Carts.CartResolver;
using Cart.Infrastructure.Persistence;
using Inventory.Contracts.Grpc;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Cart.Features.Carts.AddItem;

public record AddItemResult(Guid CartId, string? IssuedGuestToken);

internal class AddItemToCartCommandHandler(
    CartDbContext context,
    ICartResolver cartResolver,
    InventoryGrpc.InventoryGrpcClient inventoryClient)
    : IRequestHandler<AddItemToCartCommand, AddItemResult>
{
    public async Task<AddItemResult> Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
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

        // Resolve or create cart
        var resolved = await cartResolver.ResolveAsync(createIfMissing: true, cancellationToken);

        resolved.Cart.AddItem(request.ProductId, request.Quantity, 99.99m);
        await context.SaveChangesAsync(cancellationToken);

        return new AddItemResult(resolved.Cart.Id, resolved.NewlyIssuedToken);
    }
}