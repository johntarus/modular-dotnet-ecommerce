using System.Security.Cryptography;
using BuildingBlocks.Exceptions;
using BuildingBlocks.Infrastructure.CurrentUser;
using Cart.Domain.Entities;
using Cart.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Cart.Features.Carts.CartResolver;

public record ResolvedCart(ShoppingCart Cart, string? NewlyIssuedToken);

public interface ICartResolver
{
    Task<ResolvedCart> ResolveAsync(bool createIfMissing = true, CancellationToken cancellationToken = default);
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

    public async Task<ResolvedCart> ResolveAsync(bool createIfMissing = true,
        CancellationToken cancellationToken = default)
    {
        // 1. Authenticated user → find or create by UserId
        if (currentUser.UserId is { } userId && userId != Guid.Empty)
        {
            var userCart = await context.Carts.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            if (userCart is not null) return new ResolvedCart(userCart, null);
            
            if(!createIfMissing)
                throw new NotFoundException("Cart Not Found");
            
            userCart = ShoppingCart.CreateForUser(userId);
            context.Carts.Add(userCart);
            await context.SaveChangesAsync(cancellationToken);
            return new ResolvedCart(userCart, null);
        }
        
        //Guest
        var incomingToken = GetGuestToken();
        // No token → optionally create a new cart + issue token
        if (incomingToken is null)
        {
            if (!createIfMissing)
                throw new BadHttpRequestException("Guest users must provide X-Cart-Token or create a new cart.");
            var newToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var newCart = ShoppingCart.CreateForGuest(HashToken(newToken));
            context.Carts.Add(newCart);
            await context.SaveChangesAsync(cancellationToken);
            return new ResolvedCart(newCart, newToken);
        }
        
        // Token supplied → find by hash; never create
        var hash = HashToken(incomingToken);
        var existing = await context.Carts
                           .Include(c => c.Items)
                           .FirstOrDefaultAsync(c => c.GuestTokenHash == hash, cancellationToken)
                       ?? throw new NotFoundException("Cart not found.");
        return new ResolvedCart(existing, null);
    }
    
    internal static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }
}
