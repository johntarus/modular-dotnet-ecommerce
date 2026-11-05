using Cart.Domain.Entities;
using Cart.Features.Carts.MergeGuestCart;
using Cart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Cart.IntegrationTests.Features;

public class MergeGuestCartTests(PostgresFixture db) : CartTestBase(db)
{
    private static readonly Guid ProductA = Guid.NewGuid();
    private static readonly Guid ProductB = Guid.NewGuid();

    private async Task<MergeResult> MergeAsync(string? token)
    {
        if (token is not null) SendGuestToken(token);

        await using var context = NewContext();
        var handler = new MergeGuestCartCommandHandler(context, CurrentUser, HttpContextAccessor, Catalog, Inventory);
        return await handler.Handle(new MergeGuestCartCommand(token), CancellationToken.None);
    }

    private async Task<ShoppingCart?> LoadUserCartAsync(Guid userId)
    {
        await using var context = NewContext();
        return await context.Carts.AsNoTracking().Include(c => c.Items).SingleOrDefaultAsync(c => c.UserId == userId);
    }

    [Fact]
    public async Task Guest_IsRejected()
    {
        var (_, token) = await SeedGuestCartAsync((ProductA, 1, 10m));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => MergeAsync(token));
    }

    [Fact]
    public async Task NoToken_ReturnsWarning_AndChangesNothing()
    {
        SignIn();

        var result = await MergeAsync(null);

        Assert.Equal(0, result.MergedItemCount);
        Assert.Single(result.Warnings);
        Assert.Equal(0, await CountCartsAsync());
    }

    [Fact]
    public async Task UnknownToken_ReturnsWarning()
    {
        SignIn();

        var result = await MergeAsync("unknown-token");

        Assert.Equal(0, result.MergedItemCount);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public async Task EmptyGuestCart_ReturnsWarning_AndCreatesNoUserCart()
    {
        var userId = SignIn();
        var (guestCart, token) = await SeedGuestCartAsync();

        var result = await MergeAsync(token);

        Assert.Equal(0, result.MergedItemCount);
        Assert.Equal(Guid.Empty, result.CartId);
        Assert.Single(result.Warnings);
        Assert.Null(await LoadUserCartAsync(userId));
        // The empty guest cart is left for the cleanup job
        Assert.NotNull(await LoadCartAsync(guestCart.Id));
    }

    [Fact]
    public async Task UserWithoutCart_GetsNewCartWithGuestItems_AndGuestCartIsDeleted()
    {
        var userId = SignIn();
        var (guestCart, token) = await SeedGuestCartAsync((ProductA, 2, 10m), (ProductB, 1, 20m));
        Catalog.WithProduct(ProductA, 10m).WithProduct(ProductB, 20m);

        var result = await MergeAsync(token);

        Assert.Equal(2, result.MergedItemCount);
        Assert.Empty(result.Warnings);
        var userCart = await LoadUserCartAsync(userId);
        Assert.NotNull(userCart);
        Assert.Equal(userCart.Id, result.CartId);
        Assert.Equal(2, userCart.Items.Count);
        Assert.Null(await LoadCartAsync(guestCart.Id));
        Assert.Equal(1, await CountCartsAsync());
    }

    [Fact]
    public async Task UserWithExistingCart_ResultReturnsThatCartsId()
    {
        var userId = SignIn();
        var userCart = await SeedUserCartAsync(userId, (ProductA, 1, 10m));
        var (guestCart, token) = await SeedGuestCartAsync((ProductB, 1, 20m));
        Catalog.WithProduct(ProductB, 20m);

        var result = await MergeAsync(token);

        Assert.Equal(userCart.Id, result.CartId);
        Assert.NotEqual(guestCart.Id, result.CartId);
    }

    [Fact]
    public async Task MergedItems_UseCurrentCatalogPrice_NotGuestPrice()
    {
        var userId = SignIn();
        var (_, token) = await SeedGuestCartAsync((ProductA, 1, 10m));
        Catalog.WithProduct(ProductA, 15m);

        await MergeAsync(token);

        Assert.Equal(15m, Assert.Single((await LoadUserCartAsync(userId))!.Items).UnitPrice);
    }

    [Fact]
    public async Task SameProduct_QuantitiesAreCombined_AndStockCheckedForTotal()
    {
        var userId = SignIn();
        await SeedUserCartAsync(userId, (ProductA, 5, 10m));
        var (_, token) = await SeedGuestCartAsync((ProductA, 3, 10m));
        Catalog.WithProduct(ProductA, 10m);

        await MergeAsync(token);

        Assert.Equal(8, Assert.Single((await LoadUserCartAsync(userId))!.Items).Quantity);
        Assert.Equal(8, Assert.Single(Inventory.Requests).RequestedQuantity);
    }

    [Fact]
    public async Task TotalOverStock_IsSkippedWithWarning_OtherItemsStillMerge()
    {
        var userId = SignIn();
        await SeedUserCartAsync(userId, (ProductA, 5, 10m));
        var (guestCart, token) = await SeedGuestCartAsync((ProductA, 3, 10m), (ProductB, 1, 20m));
        Catalog.WithProduct(ProductA, 10m).WithProduct(ProductB, 20m);
        Inventory.WithStock(ProductA, 6);

        var result = await MergeAsync(token);

        Assert.Equal(1, result.MergedItemCount);
        Assert.Contains(result.Warnings, w => w.Contains(ProductA.ToString()) && w.Contains("insufficient stock"));
        var userCart = (await LoadUserCartAsync(userId))!;
        Assert.Equal(5, userCart.Items.Single(i => i.ProductId == ProductA).Quantity);
        Assert.Contains(userCart.Items, i => i.ProductId == ProductB);
        Assert.Null(await LoadCartAsync(guestCart.Id));
    }

    [Fact]
    public async Task DeletedProduct_IsSkippedWithWarning_WithoutStockCheck()
    {
        var userId = SignIn();
        var (_, token) = await SeedGuestCartAsync((ProductA, 1, 10m), (ProductB, 1, 20m));
        Catalog.WithProduct(ProductB, 20m); // ProductA no longer in catalog

        var result = await MergeAsync(token);

        Assert.Equal(1, result.MergedItemCount);
        Assert.Contains(result.Warnings, w => w.Contains(ProductA.ToString()) && w.Contains("no longer available"));
        Assert.DoesNotContain(Inventory.Requests, r => r.ProductId == ProductA.ToString());
        Assert.Equal(ProductB, Assert.Single((await LoadUserCartAsync(userId))!.Items).ProductId);
    }

    [Fact]
    public async Task CombinedQuantityOverMax_IsCappedWithWarning()
    {
        var userId = SignIn();
        await SeedUserCartAsync(userId, (ProductA, 60, 10m));
        var (_, token) = await SeedGuestCartAsync((ProductA, 60, 10m));
        Catalog.WithProduct(ProductA, 10m);

        var result = await MergeAsync(token);

        Assert.Equal(1, result.MergedItemCount);
        Assert.Contains(result.Warnings, w => w.Contains("reduced from 60 to 39"));
        Assert.Equal(CartItem.MaxQuantity, Assert.Single((await LoadUserCartAsync(userId))!.Items).Quantity);
        Assert.Equal(CartItem.MaxQuantity, Assert.Single(Inventory.Requests).RequestedQuantity);
    }

    [Fact]
    public async Task UserLineAlreadyAtMax_IsSkippedWithWarning_WithoutStockCheck()
    {
        var userId = SignIn();
        await SeedUserCartAsync(userId, (ProductA, CartItem.MaxQuantity, 10m));
        var (_, token) = await SeedGuestCartAsync((ProductA, 1, 10m));
        Catalog.WithProduct(ProductA, 10m);

        var result = await MergeAsync(token);

        Assert.Equal(0, result.MergedItemCount);
        Assert.Contains(result.Warnings, w => w.Contains("maximum"));
        Assert.Empty(Inventory.Requests);
        Assert.Equal(CartItem.MaxQuantity, Assert.Single((await LoadUserCartAsync(userId))!.Items).Quantity);
    }
}
