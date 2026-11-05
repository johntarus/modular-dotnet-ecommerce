using BuildingBlocks.Exceptions;
using Cart.Features.Carts.AddItem;
using Cart.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Cart.IntegrationTests.Features;

public class AddItemToCartTests(PostgresFixture db) : CartTestBase(db)
{
    private static readonly Guid ProductId = Guid.NewGuid();

    private async Task<AddItemResult> AddAsync(Guid productId, int quantity)
    {
        await using var context = NewContext();
        var handler = new AddItemToCartCommandHandler(context, NewResolver(context), Catalog, Inventory);
        return await handler.Handle(new AddItemToCartCommand(productId, quantity), CancellationToken.None);
    }

    [Fact]
    public async Task UsesPriceFromCatalog()
    {
        SignIn();
        Catalog.WithProduct(ProductId, 42.50m);

        var result = await AddAsync(ProductId, 2);

        var item = Assert.Single((await LoadCartAsync(result.CartId))!.Items);
        Assert.Equal(ProductId, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(42.50m, item.UnitPrice);
    }

    [Fact]
    public async Task ExistingLine_IsIncreasedAndRepricedFromCatalog()
    {
        var userId = SignIn();
        await SeedUserCartAsync(userId, (ProductId, 2, 10m));
        Catalog.WithProduct(ProductId, 12m);

        var result = await AddAsync(ProductId, 3);

        var item = Assert.Single((await LoadCartAsync(result.CartId))!.Items);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(12m, item.UnitPrice);
    }

    [Fact]
    public async Task UnknownProduct_ThrowsNotFound_WithoutStockCheckOrCart()
    {
        SignIn();

        await Assert.ThrowsAsync<NotFoundException>(() => AddAsync(ProductId, 1));
        Assert.Empty(Inventory.Requests);
        Assert.Equal(0, await CountCartsAsync());
    }

    [Fact]
    public async Task StockCheck_UsesExistingQuantityPlusNewQuantity()
    {
        var userId = SignIn();
        await SeedUserCartAsync(userId, (ProductId, 5, 10m));
        Catalog.WithProduct(ProductId, 10m);

        await AddAsync(ProductId, 3);

        Assert.Equal(8, Assert.Single(Inventory.Requests).RequestedQuantity);
    }

    [Fact]
    public async Task TotalOverStock_IsRejected_AndCartUnchanged()
    {
        var userId = SignIn();
        var cart = await SeedUserCartAsync(userId, (ProductId, 5, 10m));
        Catalog.WithProduct(ProductId, 10m);
        Inventory.WithStock(ProductId, 6);

        await Assert.ThrowsAsync<BadHttpRequestException>(() => AddAsync(ProductId, 3));

        Assert.Equal(5, Assert.Single((await LoadCartAsync(cart.Id))!.Items).Quantity);
    }

    [Fact]
    public async Task Guest_WithoutToken_GetsNewCartAndToken()
    {
        Catalog.WithProduct(ProductId, 10m);

        var result = await AddAsync(ProductId, 1);

        Assert.False(string.IsNullOrWhiteSpace(result.IssuedGuestToken));
        var stored = await LoadCartAsync(result.CartId);
        Assert.NotNull(stored);
        Assert.Null(stored.UserId);
        Assert.Single(stored.Items);
    }

    [Fact]
    public async Task Guest_SendingTokenBack_AddsToSameCartAndGetsNoNewToken()
    {
        Catalog.WithProduct(ProductId, 10m);
        var other = Guid.NewGuid();
        Catalog.WithProduct(other, 20m);

        var first = await AddAsync(ProductId, 1);
        SendGuestToken(first.IssuedGuestToken!);
        var second = await AddAsync(other, 1);

        Assert.Equal(first.CartId, second.CartId);
        Assert.Null(second.IssuedGuestToken);
        Assert.Equal(2, (await LoadCartAsync(first.CartId))!.Items.Count);
        Assert.Equal(1, await CountCartsAsync());
    }
}
