using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.CurrentUser;
using Cart.Domain.Entities;
using Cart.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.CartResolver;

public interface ICartResolver
{
    Task<ShoppingCart> ResolveAsync(bool createIfMissing = true, CancellationToken cancellationToken = default);
    string? GetGuestToken();
}

internal sealed class CartResolver(
    CartDbContext context,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ICartResolver
{
    public string? GetGuestToken()
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers["X-Cart-Token"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(header) ? null : header;
    }

    public async Task<ShoppingCart> ResolveAsync(bool createIfMissing = true,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        // 1. Authenticated user → find or create by UserId
        if (userId != Guid.Empty)
        {
            var userCart = await context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (userCart is not null) return userCart;
            
            if(!createIfMissing)
                throw new NotFoundException("Cart Not Found");
            
            userCart = ShoppingCart.CreateForUser(userId);
            context.Carts.Add(userCart);
            await context.SaveChangesAsync(cancellationToken);
            return userCart;
        }
        
        // 2. Guest → find or create by GuestToken
        var guestTokenHash = GetGuestToken()
            ?? throw new BadHttpRequestException("Guest Users must provide X-Cart-Token header");

        var guestCart = await context.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.GuestTokenHash == guestTokenHash, cancellationToken);

        if (guestCart != null) return guestCart;
        
        if(!createIfMissing)
            throw new NotFoundException("Cart Not Found");

        guestCart = ShoppingCart.CreateForGuest(guestTokenHash);
        context.Carts.Add(guestCart);
        await context.SaveChangesAsync(cancellationToken);
        return guestCart;
    }
}