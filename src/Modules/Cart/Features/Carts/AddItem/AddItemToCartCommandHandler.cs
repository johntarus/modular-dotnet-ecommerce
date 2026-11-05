using BuildingBlocks.Exceptions;
using Cart.Features.Carts.CartResolver;
using Cart.Infrastructure.Persistence;
using Catalog.Contracts;
using Inventory.Contracts.Grpc;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Cart.Features.Carts.AddItem;

public record AddItemResult(Guid CartId, string? IssuedGuestToken);

internal class AddItemToCartCommandHandler(
    CartDbContext context,
    ICartResolver cartResolver,
    ICatalogApi catalogApi,
    InventoryGrpc.InventoryGrpcClient inventoryClient)
    : IRequestHandler<AddItemToCartCommand, AddItemResult>
{
    public async Task<AddItemResult> Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
    {
        // 1. Product must exist in the catalog; its current price is the source of truth
        var product = await catalogApi.GetProductByIdAsync(request.ProductId, cancellationToken)
                      ?? throw new NotFoundException($"Product {request.ProductId} was not found.");

        // 2. Resolve or create cart
        var resolved = await cartResolver.ResolveAsync(createIfMissing: true, cancellationToken);

        // 3. gRPC stock check against the resulting total for this product
        var existingQuantity = resolved.Cart.Items
            .FirstOrDefault(i => i.ProductId == request.ProductId)?.Quantity ?? 0;

        var stockResponse = await inventoryClient.CheckAvailabilityAsync(
            new CheckAvailabilityRequest
            {
                ProductId = request.ProductId.ToString(),
                RequestedQuantity = existingQuantity + request.Quantity
            }, cancellationToken: cancellationToken);

        if (!stockResponse.IsAvailable)
            throw new BadHttpRequestException($"Cannot add to cart: {stockResponse.Message}");

        resolved.Cart.AddItem(request.ProductId, request.Quantity, product.Price);
        await context.SaveChangesAsync(cancellationToken);

        return new AddItemResult(resolved.Cart.Id, resolved.NewlyIssuedToken);
    }
}
