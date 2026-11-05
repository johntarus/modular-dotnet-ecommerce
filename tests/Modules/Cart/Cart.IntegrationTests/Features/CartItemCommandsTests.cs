using BuildingBlocks.Exceptions;
using Cart.Features.Carts.ClearCart;
using Cart.Features.Carts.GetCart;
using Cart.Features.Carts.RemoveItem;
using Cart.Features.Carts.SetItemQuantity;
using Cart.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Cart.IntegrationTests.Features;

public class SetItemQuantityTests(PostgresFixture db) : CartTestBase(db)
{
    private static readonly Guid ProductId = Guid.NewGuid();

    private async Task SetAsync(Guid productId, int quantity)
    {
        await using var context = NewContext();
        var handler = new SetItemQuantityCommandHandler(context, CurrentUser, Inventory);
        await handler.Handle(new SetItemQuantityCommand(productId, quantity), CancellationToken.None);
    }

    [Fact]
    public async Task UpdatesQuantity_AndChecksStockForNewTotal()
    {
        var userId = SignIn();
        var cart = await SeedUserCartAsync(userId, (ProductId, 2, 10m));

        await SetAsync(ProductId, 7);

        Assert.Equal(7, Assert.Single((await LoadCartAsync(cart.Id))!.Items).Quantity);
        Assert.Equal(7, Assert.Single(Inventory.Requests).RequestedQuantity);
    }

    [Fact]
    public async Task InsufficientStock_IsRejected_AndQuantityUnchanged()
    {
        var userId = SignIn();
        var cart = await SeedUserCartAsync(userId, (ProductId, 2, 10m));
        Inventory.WithStock(ProductId, 5);

        await Assert.ThrowsAsync<BadHttpRequestException>(() => SetAsync(ProductId, 6));

        Assert.Equal(2, Assert.Single((await LoadCartAsync(cart.Id))!.Items).Quantity);
    }

    [Fact]
    public async Task ItemNotInCart_ThrowsNotFound_WithoutStockCheck()
    {
        var userId = SignIn();
        await SeedUserCartAsync(userId, (ProductId, 2, 10m));

        await Assert.ThrowsAsync<NotFoundException>(() => SetAsync(Guid.NewGuid(), 1));
        Assert.Empty(Inventory.Requests);
    }

    [Fact]
    public async Task UserWithoutCart_ThrowsNotFound()
    {
        SignIn();

        await Assert.ThrowsAsync<NotFoundException>(() => SetAsync(ProductId, 1));
    }

    [Fact]
    public async Task Guest_IsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => SetAsync(ProductId, 1));
    }
}

public class RemoveItemTests(PostgresFixture db) : CartTestBase(db)
{
    private async Task RemoveAsync(Guid productId)
    {
        await using var context = NewContext();
        await new RemoveItemCommandHandler(context, CurrentUser)
            .Handle(new RemoveItemCommand(productId), CancellationToken.None);
    }

    [Fact]
    public async Task RemovesOnlyThatItem()
    {
        var userId = SignIn();
        var keep = Guid.NewGuid();
        var remove = Guid.NewGuid();
        var cart = await SeedUserCartAsync(userId, (keep, 1, 10m), (remove, 2, 20m));

        await RemoveAsync(remove);

        Assert.Equal(keep, Assert.Single((await LoadCartAsync(cart.Id))!.Items).ProductId);
    }

    [Fact]
    public async Task ItemNotInCart_ThrowsNotFound()
    {
        var userId = SignIn();
        await SeedUserCartAsync(userId, (Guid.NewGuid(), 1, 10m));

        await Assert.ThrowsAsync<NotFoundException>(() => RemoveAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Guest_IsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => RemoveAsync(Guid.NewGuid()));
    }
}

public class ClearCartTests(PostgresFixture db) : CartTestBase(db)
{
    private async Task ClearAsync()
    {
        await using var context = NewContext();
        await new ClearCartCommandHandler(context, CurrentUser).Handle(new ClearCartCommand(), CancellationToken.None);
    }

    [Fact]
    public async Task RemovesAllItems_ButKeepsCart()
    {
        var userId = SignIn();
        var cart = await SeedUserCartAsync(userId, (Guid.NewGuid(), 1, 10m), (Guid.NewGuid(), 2, 20m));

        await ClearAsync();

        var stored = await LoadCartAsync(cart.Id);
        Assert.NotNull(stored);
        Assert.Empty(stored.Items);
    }

    [Fact]
    public async Task UserWithoutCart_ThrowsNotFound()
    {
        SignIn();

        await Assert.ThrowsAsync<NotFoundException>(ClearAsync);
    }

    [Fact]
    public async Task Guest_IsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(ClearAsync);
    }
}

public class GetCartTests(PostgresFixture db) : CartTestBase(db)
{
    private async Task<CartResponse> GetAsync()
    {
        await using var context = NewContext();
        return await new GetCartQueryHandler(context, NewResolver(context), CurrentUser)
            .Handle(new GetCartQuery(), CancellationToken.None);
    }

    [Fact]
    public async Task Guest_WithoutToken_GetsEmptyCart_AndNothingIsCreated()
    {
        var response = await GetAsync();

        Assert.Equal(Guid.Empty, response.Id);
        Assert.Empty(response.Items);
        Assert.Equal(0, await CountCartsAsync());
    }

    [Fact]
    public async Task User_GetsLineTotalsAndCartTotal()
    {
        var userId = SignIn();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var cart = await SeedUserCartAsync(userId, (a, 2, 10m), (b, 3, 1.50m));

        var response = await GetAsync();

        Assert.Equal(cart.Id, response.Id);
        Assert.Equal(20m, response.Items.Single(i => i.ProductId == a).Total);
        Assert.Equal(4.50m, response.Items.Single(i => i.ProductId == b).Total);
        Assert.Equal(24.50m, response.Subtotal);
    }

    [Fact]
    public async Task Guest_WithToken_GetsTheirCart()
    {
        var (cart, token) = await SeedGuestCartAsync((Guid.NewGuid(), 1, 5m));
        SendGuestToken(token);

        var response = await GetAsync();

        Assert.Equal(cart.Id, response.Id);
        Assert.Single(response.Items);
    }

    [Fact]
    public async Task UserWithoutCart_ThrowsNotFound()
    {
        SignIn();

        await Assert.ThrowsAsync<NotFoundException>(GetAsync);
    }

    [Fact]
    public async Task Guest_WithTokenForEmptyCart_GetsEmptyItemsAndZeroSubtotal()
    {
        var (cart, token) = await SeedGuestCartAsync();
        SendGuestToken(token);

        var response = await GetAsync();

        Assert.Equal(cart.Id, response.Id);
        Assert.Null(response.UserId);
        Assert.Empty(response.Items);
        Assert.Equal(0m, response.Subtotal);
    }

    [Fact]
    public async Task Guest_WithUnknownToken_ThrowsNotFound()
    {
        SendGuestToken("unknown-token");

        await Assert.ThrowsAsync<NotFoundException>(GetAsync);
    }
}
