using BuildingBlocks.Infrastructure.CurrentUser;
using Cart.Domain.Entities;
using Cart.Infrastructure.Persistence;
using Inventory.Contracts.Grpc;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.MergeGuestCart;

internal sealed class MergeGuestCartCommandHandler(
    CartDbContext context,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor,
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
            // Re-check current stock at merge time
            var stock = await inventoryClient.CheckAvailabilityAsync(
                new CheckAvailabilityRequest
                {
                    ProductId = guestItem.ProductId.ToString(),
                    RequestedQuantity = guestItem.Quantity
                }, cancellationToken: cancellationToken);
            if (!stock.IsAvailable)
            {
                warnings.Add($"Item {guestItem.ProductId} skipped: insufficient stock {stock.Message}.");
                continue;
            }
            
            userCart.AddItem(guestItem.ProductId, guestItem.Quantity, guestItem.UnitPrice);
            mergedCount++;
        }
        
        // 4. Delete guest cart
        context.Remove(guestCart);
        await context.SaveChangesAsync(cancellationToken);
        return new MergeResult(guestCart.Id, mergedCount, warnings);
            
    }
}