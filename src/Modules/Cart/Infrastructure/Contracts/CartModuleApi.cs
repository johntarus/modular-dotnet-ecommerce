using Cart.Contracts;
using Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Contracts;

// Internal implementation — registered in DI as ICartModuleApi.
internal sealed class CartModuleApi(CartDbContext context) : ICartModuleApi
{
    /// <summary>
    /// Returns the cart only if it belongs to the user (carts are keyed by the identity
    /// subject, i.e. ShoppingCart.UserId) and has at least one item; otherwise null.
    /// Guest carts are never returned — checkout requires a signed-in user.
    /// </summary>
    public async Task<CartCheckoutDto?> GetCartForCheckoutAsync(
        Guid cartId, string identitySubject, CancellationToken ct = default)
    {
        if (!Guid.TryParse(identitySubject, out var userId) || userId == Guid.Empty)
            return null;

        var cart = await context.Carts
            .AsNoTracking()
            .Where(c => c.Id == cartId && c.UserId == userId)
            .Select(c => new CartCheckoutDto(
                c.Id,
                c.Items.Select(i => new CartItemDto(i.ProductId, i.Quantity)).ToList()))
            .FirstOrDefaultAsync(ct);

        return cart is { Items.Count: > 0 } ? cart : null;
    }
}
