using BuildingBlocks.Infrastructure.CurrentUser;
using Cart.Domain.Entities;
using Cart.Infrastructure.Persistence;
using Catalog.Contracts;
using Inventory.Contracts.Grpc;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.MergeGuestCart;

internal sealed class MergeGuestCartCommandHandler(
    CartDbContext context,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor,
    ICatalogApi catalogApi,
    InventoryGrpc.InventoryGrpcClient inventoryClient) : IRequestHandler<MergeGuestCartCommand, MergeResult>
{
    public async Task<MergeResult> Handle(MergeGuestCartCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || userId == Guid.Empty)
            throw new UnauthorizedAccessException("Authentication Required.");

        var guestToken = httpContextAccessor.HttpContext?.Request.Headers["X-Cart-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(guestToken))
            return new MergeResult(Guid.Empty, 0, new List<string> { "No guest cart to merge" });

        // 1. Find guest cart (stored by hash, never by raw token)
        var guestTokenHash = CartResolver.CartResolver.HashToken(guestToken);
        var guestCart = await context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.GuestTokenHash == guestTokenHash, cancellationToken);

        if (guestCart == null || guestCart.Items.Count == 0)
            return new MergeResult(Guid.Empty, 0, new List<string> { "No Items in the guest cart" });
        
        // 2. Find or create user's cart
        var userCart = await context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (userCart == null)
        {
            userCart = ShoppingCart.CreateForUser(userId);
            context.Add(userCart);
        }
        
        // 3. Merge with stock validation
        var warnings = new List<string>();
        int mergedCount = 0;

        foreach (var guestItem in guestCart.Items)
        {
            // Re-price from the catalog; the guest cart's stored price may be stale
            var product = await catalogApi.GetProductByIdAsync(guestItem.ProductId, cancellationToken);
            if (product is null)
            {
                warnings.Add($"Item {guestItem.ProductId} skipped: product is no longer available.");
                continue;
            }

            // Cap at the per-item limit instead of letting the whole merge fail
            var existingQuantity = userCart.Items
                .FirstOrDefault(i => i.ProductId == guestItem.ProductId)?.Quantity ?? 0;
            var quantityToAdd = Math.Min(guestItem.Quantity, CartItem.MaxQuantity - existingQuantity);
            if (quantityToAdd <= 0)
            {
                warnings.Add($"Item {guestItem.ProductId} skipped: cart already holds the maximum of {CartItem.MaxQuantity}.");
                continue;
            }

            // Re-check current stock at merge time against the resulting total in the user's cart
            var stock = await inventoryClient.CheckAvailabilityAsync(
                new CheckAvailabilityRequest
                {
                    ProductId = guestItem.ProductId.ToString(),
                    RequestedQuantity = existingQuantity + quantityToAdd
                }, cancellationToken: cancellationToken);
            if (!stock.IsAvailable)
            {
                warnings.Add($"Item {guestItem.ProductId} skipped: insufficient stock {stock.Message}.");
                continue;
            }
            
            userCart.AddItem(guestItem.ProductId, quantityToAdd, product.Price);
            if (quantityToAdd < guestItem.Quantity)
                warnings.Add($"Item {guestItem.ProductId} quantity reduced from {guestItem.Quantity} to {quantityToAdd}: maximum per item is {CartItem.MaxQuantity}.");
            mergedCount++;
        }
        
        // 4. Delete guest cart
        context.Remove(guestCart);
        await context.SaveChangesAsync(cancellationToken);
        return new MergeResult(userCart.Id, mergedCount, warnings);
            
    }
}