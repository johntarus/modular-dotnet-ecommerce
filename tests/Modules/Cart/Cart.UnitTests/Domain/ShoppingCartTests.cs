using BuildingBlocks.Exceptions;
using Cart.Domain.Entities;
using ValidationException = BuildingBlocks.Exceptions.ValidationException;

namespace Cart.UnitTests.Domain;

public class ShoppingCartTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProductA = Guid.NewGuid();
    private static readonly Guid ProductB = Guid.NewGuid();

    // ---------- Creation ----------

    [Fact]
    public void CreateForUser_SetsUserAndNoGuestToken()
    {
        var cart = ShoppingCart.CreateForUser(UserId);

        Assert.NotEqual(Guid.Empty, cart.Id);
        Assert.Equal(UserId, cart.UserId);
        Assert.Null(cart.GuestTokenHash);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void CreateForUser_WithEmptyUserId_Throws()
    {
        Assert.Throws<ValidationException>(() => ShoppingCart.CreateForUser(Guid.Empty));
    }

    [Fact]
    public void CreateForGuest_SetsTokenHashAndExpiresIn24Hours()
    {
        var before = DateTime.UtcNow;

        var cart = ShoppingCart.CreateForGuest("hash-123");

        Assert.Null(cart.UserId);
        Assert.Equal("hash-123", cart.GuestTokenHash);
        Assert.Empty(cart.Items);
        Assert.InRange(cart.ExpiresAt, before.AddHours(24), DateTime.UtcNow.AddHours(24));
    }

    [Fact]
    public void Create_GivesEachCartAUniqueId()
    {
        var first = ShoppingCart.CreateForUser(UserId);
        var second = ShoppingCart.CreateForUser(UserId);

        Assert.NotEqual(first.Id, second.Id);
    }

    // ---------- Guest expiry (sliding) ----------

    public static TheoryData<string> Changes => new() { "add", "set", "remove", "clear" };

    private static void Apply(ShoppingCart cart, string change)
    {
        switch (change)
        {
            case "add": cart.AddItem(ProductB, 1, 5m); break;
            case "set": cart.SetItemQuantity(ProductA, 3); break;
            case "remove": cart.RemoveItem(ProductA); break;
            case "clear": cart.Clear(); break;
        }
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public void GuestCart_EveryChange_PushesExpiryTo24HoursAfterThatChange(string change)
    {
        var cart = ShoppingCart.CreateForGuest("hash");
        cart.AddItem(ProductA, 1, 10m);

        Apply(cart, change);

        Assert.Equal(cart.UpdatedAt.Add(ShoppingCart.GuestCartLifetime), cart.ExpiresAt);
    }

    [Fact]
    public void GuestCart_ExpiryMovesForward_WhenChangedLater()
    {
        var cart = ShoppingCart.CreateForGuest("hash");
        var initialExpiry = cart.ExpiresAt;
        Thread.Sleep(5);

        cart.AddItem(ProductA, 1, 10m);

        Assert.True(cart.ExpiresAt > initialExpiry);
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public void UserCart_NeverGetsAnExpiry(string change)
    {
        var cart = ShoppingCart.CreateForUser(UserId);
        cart.AddItem(ProductA, 1, 10m);

        Apply(cart, change);

        Assert.Equal(default, cart.ExpiresAt);
    }

    [Fact]
    public void GuestCart_ClearingAnEmptyCart_DoesNotExtendExpiry()
    {
        var cart = ShoppingCart.CreateForGuest("hash");
        var expiry = cart.ExpiresAt;
        Thread.Sleep(5);

        cart.Clear();

        Assert.Equal(expiry, cart.ExpiresAt);
    }

    // ---------- AddItem ----------

    [Fact]
    public void AddItem_NewProduct_AddsLine()
    {
        var cart = ShoppingCart.CreateForUser(UserId);

        cart.AddItem(ProductA, 2, 10m);

        var item = Assert.Single(cart.Items);
        Assert.Equal(ProductA, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(10m, item.UnitPrice);
    }

    [Fact]
    public void AddItem_ExistingProduct_IncreasesQuantityAndUsesLatestPrice()
    {
        var cart = ShoppingCart.CreateForUser(UserId);
        cart.AddItem(ProductA, 2, 10m);

        cart.AddItem(ProductA, 3, 12m);

        var item = Assert.Single(cart.Items);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(12m, item.UnitPrice);
    }

    [Fact]
    public void AddItem_DifferentProducts_AddsSeparateLines()
    {
        var cart = ShoppingCart.CreateForUser(UserId);

        cart.AddItem(ProductA, 1, 10m);
        cart.AddItem(ProductB, 1, 20m);

        Assert.Equal(2, cart.Items.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_WithNonPositiveQuantity_Throws(int quantity)
    {
        var cart = ShoppingCart.CreateForUser(UserId);

        Assert.Throws<ValidationException>(() => cart.AddItem(ProductA, quantity, 10m));
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void AddItem_WithNegativePrice_Throws()
    {
        var cart = ShoppingCart.CreateForUser(UserId);

        Assert.Throws<ValidationException>(() => cart.AddItem(ProductA, 1, -1m));
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void AddItem_PushingExistingLineOverMax_ThrowsAndKeepsOriginalQuantity()
    {
        var cart = ShoppingCart.CreateForUser(UserId);
        cart.AddItem(ProductA, CartItem.MaxQuantity, 10m);

        Assert.Throws<ValidationException>(() => cart.AddItem(ProductA, 1, 10m));
        Assert.Equal(CartItem.MaxQuantity, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void AddItem_UpdatesUpdatedAt()
    {
        var cart = ShoppingCart.CreateForUser(UserId);
        var before = cart.UpdatedAt;

        cart.AddItem(ProductA, 1, 10m);

        Assert.True(cart.UpdatedAt >= before);
    }

    // ---------- SetItemQuantity ----------

    [Fact]
    public void SetItemQuantity_ExistingItem_ReplacesQuantity()
    {
        var cart = ShoppingCart.CreateForUser(UserId);
        cart.AddItem(ProductA, 5, 10m);

        cart.SetItemQuantity(ProductA, 2);

        Assert.Equal(2, Assert.Single(cart.Items).Quantity);
    }

    [Fact]
    public void SetItemQuantity_MissingItem_ThrowsNotFound()
    {
        var cart = ShoppingCart.CreateForUser(UserId);
        cart.AddItem(ProductA, 1, 10m);

        Assert.Throws<NotFoundException>(() => cart.SetItemQuantity(ProductB, 2));
    }

    [Fact]
    public void SetItemQuantity_WithEmptyProductId_Throws()
    {
        var cart = ShoppingCart.CreateForUser(UserId);

        Assert.Throws<ValidationException>(() => cart.SetItemQuantity(Guid.Empty, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(CartItem.MaxQuantity + 1)]
    public void SetItemQuantity_OutOfRange_Throws(int quantity)
    {
        var cart = ShoppingCart.CreateForUser(UserId);
        cart.AddItem(ProductA, 5, 10m);

        Assert.Throws<ValidationException>(() => cart.SetItemQuantity(ProductA, quantity));
        Assert.Equal(5, Assert.Single(cart.Items).Quantity);
    }

    // ---------- RemoveItem ----------

    [Fact]
    public void RemoveItem_ExistingItem_RemovesOnlyThatLine()
    {
        var cart = ShoppingCart.CreateForUser(UserId);
        cart.AddItem(ProductA, 1, 10m);
        cart.AddItem(ProductB, 1, 20m);

        cart.RemoveItem(ProductA);

        Assert.Equal(ProductB, Assert.Single(cart.Items).ProductId);
    }

    [Fact]
    public void RemoveItem_MissingItem_ThrowsNotFound()
    {
        var cart = ShoppingCart.CreateForUser(UserId);

        Assert.Throws<NotFoundException>(() => cart.RemoveItem(ProductA));
    }

    [Fact]
    public void RemoveItem_WithEmptyProductId_Throws()
    {
        var cart = ShoppingCart.CreateForUser(UserId);

        Assert.Throws<ValidationException>(() => cart.RemoveItem(Guid.Empty));
    }

    // ---------- Clear ----------

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var cart = ShoppingCart.CreateForUser(UserId);
        cart.AddItem(ProductA, 1, 10m);
        cart.AddItem(ProductB, 2, 20m);

        cart.Clear();

        Assert.Empty(cart.Items);
    }

    [Fact]
    public void Clear_EmptyCart_DoesNotTouchUpdatedAt()
    {
        var cart = ShoppingCart.CreateForUser(UserId);
        var before = cart.UpdatedAt;

        cart.Clear();

        Assert.Equal(before, cart.UpdatedAt);
    }
}
