using Cart.Domain.Entities;
using ValidationException = BuildingBlocks.Exceptions.ValidationException;

namespace Cart.UnitTests.Domain;

public class CartItemTests
{
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var item = new CartItem(ProductId, 3, 12.50m);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(ProductId, item.ProductId);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(12.50m, item.UnitPrice);
    }

    [Fact]
    public void Constructor_WithEmptyProductId_Throws()
    {
        Assert.Throws<ValidationException>(() => new CartItem(Guid.Empty, 1, 10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(CartItem.MaxQuantity + 1)]
    public void Constructor_WithQuantityOutOfRange_Throws(int quantity)
    {
        Assert.Throws<ValidationException>(() => new CartItem(ProductId, quantity, 10m));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(CartItem.MaxQuantity)]
    public void Constructor_WithQuantityAtBoundaries_Succeeds(int quantity)
    {
        var item = new CartItem(ProductId, quantity, 10m);

        Assert.Equal(quantity, item.Quantity);
    }

    [Fact]
    public void Constructor_WithNegativePrice_Throws()
    {
        Assert.Throws<ValidationException>(() => new CartItem(ProductId, 1, -0.01m));
    }

    [Fact]
    public void Constructor_WithZeroPrice_Succeeds()
    {
        var item = new CartItem(ProductId, 1, 0m);

        Assert.Equal(0m, item.UnitPrice);
    }

    [Fact]
    public void Increase_AddsQuantityAndUpdatesPrice()
    {
        var item = new CartItem(ProductId, 2, 10m);

        item.Increase(3, 8m);

        Assert.Equal(5, item.Quantity);
        Assert.Equal(8m, item.UnitPrice);
    }

    [Fact]
    public void Increase_UpToMaxQuantity_Succeeds()
    {
        var item = new CartItem(ProductId, CartItem.MaxQuantity - 1, 10m);

        item.Increase(1, 10m);

        Assert.Equal(CartItem.MaxQuantity, item.Quantity);
    }

    [Fact]
    public void Increase_BeyondMaxQuantity_ThrowsAndLeavesItemUnchanged()
    {
        var item = new CartItem(ProductId, CartItem.MaxQuantity, 10m);

        Assert.Throws<ValidationException>(() => item.Increase(1, 5m));
        Assert.Equal(CartItem.MaxQuantity, item.Quantity);
        Assert.Equal(10m, item.UnitPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Increase_WithNonPositiveQuantity_Throws(int quantity)
    {
        var item = new CartItem(ProductId, 1, 10m);

        Assert.Throws<ValidationException>(() => item.Increase(quantity, 10m));
    }

    [Fact]
    public void Increase_WithNegativePrice_Throws()
    {
        var item = new CartItem(ProductId, 1, 10m);

        Assert.Throws<ValidationException>(() => item.Increase(1, -1m));
    }

    [Fact]
    public void SetQuantity_ReplacesQuantity()
    {
        var item = new CartItem(ProductId, 5, 10m);

        item.SetQuantity(2);

        Assert.Equal(2, item.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(CartItem.MaxQuantity + 1)]
    public void SetQuantity_OutOfRange_ThrowsAndLeavesQuantityUnchanged(int quantity)
    {
        var item = new CartItem(ProductId, 5, 10m);

        Assert.Throws<ValidationException>(() => item.SetQuantity(quantity));
        Assert.Equal(5, item.Quantity);
    }
}
