using Cart.Domain.Entities;
using Cart.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Resolver = Cart.Features.Carts.CartResolver.CartResolver;

namespace Cart.IntegrationTests.Infrastructure;

[Collection(PostgresCollection.Name)]
public abstract class CartTestBase(PostgresFixture db) : IAsyncLifetime
{
    protected PostgresFixture Db { get; } = db;
    protected FakeCurrentUser CurrentUser { get; } = new();
    protected FakeCatalogApi Catalog { get; } = new();
    protected FakeInventoryClient Inventory { get; } = new();
    protected HttpContextAccessor HttpContextAccessor { get; } = new() { HttpContext = new DefaultHttpContext() };

    public Task InitializeAsync() => Db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    internal CartDbContext NewContext() => Db.CreateContext();

    internal Resolver NewResolver(CartDbContext context) => new(context, CurrentUser, HttpContextAccessor);

    protected Guid SignIn()
    {
        var userId = Guid.NewGuid();
        CurrentUser.UserId = userId;
        return userId;
    }

    protected void SendGuestToken(string token) =>
        HttpContextAccessor.HttpContext!.Request.Headers["X-Cart-Token"] = token;

    protected async Task<ShoppingCart> SeedUserCartAsync(Guid userId, params (Guid ProductId, int Quantity, decimal Price)[] items)
    {
        var cart = ShoppingCart.CreateForUser(userId);
        foreach (var (productId, quantity, price) in items)
            cart.AddItem(productId, quantity, price);

        await using var context = NewContext();
        context.Carts.Add(cart);
        await context.SaveChangesAsync();
        return cart;
    }

    /// <summary>Seeds a guest cart and returns the raw token a client would send.</summary>
    protected async Task<(ShoppingCart Cart, string Token)> SeedGuestCartAsync(params (Guid ProductId, int Quantity, decimal Price)[] items)
    {
        var token = Guid.NewGuid().ToString("N");
        var cart = ShoppingCart.CreateForGuest(Resolver.HashToken(token));
        foreach (var (productId, quantity, price) in items)
            cart.AddItem(productId, quantity, price);

        await using var context = NewContext();
        context.Carts.Add(cart);
        await context.SaveChangesAsync();
        return (cart, token);
    }

    /// <summary>Reads a cart straight from the database, bypassing any tracked state.</summary>
    protected async Task<ShoppingCart?> LoadCartAsync(Guid cartId)
    {
        await using var context = NewContext();
        return await context.Carts.AsNoTracking().Include(c => c.Items).SingleOrDefaultAsync(c => c.Id == cartId);
    }

    protected async Task<int> CountCartsAsync()
    {
        await using var context = NewContext();
        return await context.Carts.CountAsync();
    }
}
